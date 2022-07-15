// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Parameters;
using Microsoft.TemplateEngine.Core.Expressions.Cpp2;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Core
{
#pragma warning disable CS0618 // Type or member is obsolete
    public class ParameterSetBuilder : ParametersDefinition, IParameterSetBuilder, IParameterSet
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly Dictionary<ITemplateParameter, EvalData> _resolvedValues;
        private IEvaluatedParameterSetData? _result;

        internal ParameterSetBuilder(IReadOnlyDictionary<string, ITemplateParameter> parameters) : base(parameters)
        {
            _resolvedValues = parameters.ToDictionary(p => p.Value, p => new EvalData(p.Value));
        }

        internal ParameterSetBuilder(IParametersDefinition parameters) : this(parameters.AsReadonlyDictionary())
        { }

        public IEnumerable<ITemplateParameter> ParameterDefinitions => this;

        public IDictionary<ITemplateParameter, object> ResolvedValues =>
            _resolvedValues
                .Where(p => p.Value.Value != null)
                .ToDictionary(k => k.Key, k => k.Value.Value!);

#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
        public static IParameterSetBuilder CreateWithDefaults(IParametersDefinition parametersDefinition, IEngineEnvironmentSettings environment, string? name = null)
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
        {
            var result = CreateWithDefaults(parametersDefinition, name, environment, out IReadOnlyList<string> errors);
            if (errors.Any())
            {
                throw new Exception("Parameters with errors encountered: " + errors.ToCsvString());
            }

            return result;
        }

        public static IParameterSetBuilder CreateWithDefaults(IParametersDefinition parametersDefinition, string? name, IEngineEnvironmentSettings environment, out IReadOnlyList<string> paramsWithInvalidValues)
        {
            IParameterSetBuilder templateParams = new ParameterSetBuilder(parametersDefinition);
            List<string> paramsWithInvalidValuesList = new List<string>();

            foreach (ITemplateParameter param in templateParams)
            {
                if (param.IsName)
                {
                    if (name != null)
                    {
                        templateParams.SetParameterValue(param, name);
                    }
                }
                else
                {
                    ParameterConverter.SetParameterDefault(
                        templateParams,
                        param,
                        environment,
                        true,
                        param.Precedence.CanBeRequired,
                        paramsWithInvalidValuesList);
                }
            }

            paramsWithInvalidValues = paramsWithInvalidValuesList;
            return templateParams;
        }

        public bool TryGetParameterDefinition(string name, out ITemplateParameter parameter) => this.TryGetParameterDefinition(name, out parameter);

        public void SetParameterValue(ITemplateParameter parameter, object value)
        {
            _resolvedValues[parameter].Value = value;
            _result = null;
        }

        public void SetParameterEvaluation(ITemplateParameter parameter, EvaluatedParameterData evaluatedParameterData)
        {
            var old = _resolvedValues[parameter];
            _resolvedValues[parameter] = new EvalData(evaluatedParameterData);
            if (old.InputDataState != InputDataState.Unset)
            {
                _resolvedValues[parameter].Value = old.Value;
            }

            _result = null;
        }

        public bool HasParameterValue(ITemplateParameter parameter) => _resolvedValues[parameter].InputDataState != InputDataState.Unset;

        public void EvaluateConditionalParameters(ILogger logger)
        {
            List<EvalData> evaluatedParameters = _resolvedValues.Values.ToList();

            var variableCollection = new VariableCollection(
                null,
                evaluatedParameters
                    .Where(p => p.Value != null)
                    .ToDictionary(p => p.ParameterDefinition.Name, p => p.Value));

            EvalData[] variableCollectionIdxToParametersMap =
                evaluatedParameters.Where(p => p.Value != null).Select(p => p).ToArray();

            EvaluateEnablementConditions(evaluatedParameters, variableCollection, variableCollectionIdxToParametersMap, logger);
            EvaluateRequirementCondition(evaluatedParameters, variableCollection, logger);
        }

        public IEvaluatedParameterSetData Build()
        {
            if (_result == null)
            {
                _result = new EvaluatedParameterSetData(
                    this,
                    _resolvedValues.Select(p => p.Value.ToParameterData()).ToList());
            }

            return _result!;
        }

        private void EvaluateEnablementConditions(
            IReadOnlyList<EvalData> parameters,
            VariableCollection variableCollection,
            EvalData[] variableCollectionIdxToParametersMap,
            ILogger logger)
        {
            Dictionary<EvalData, HashSet<EvalData>> parametersDependencies = new();

            // First parameters traversal.
            //   - evaluate all IsEnabledCondition - and get the dependecies between the parameters during doing so
            foreach (EvalData parameter in parameters)
            {
                if (!string.IsNullOrEmpty(parameter.ParameterDefinition.Precedence.IsEnabledCondition))
                {
                    HashSet<int> referencedVariablesIndexes = new HashSet<int>();
                    // Do not remove from the variable collection though - we want to capture all dependencies between parameters in the first traversal.
                    // Those will be bulk removed before second traversal (traversing only the required dependencies).
                    parameter.IsEnabledConditionResult =
                        Cpp2StyleEvaluatorDefinition.EvaluateFromString(logger, parameter.ParameterDefinition.Precedence.IsEnabledCondition, variableCollection, referencedVariablesIndexes);

                    if (referencedVariablesIndexes.Any())
                    {
                        parametersDependencies[parameter] = new HashSet<EvalData>(
                            referencedVariablesIndexes.Select(idx => variableCollectionIdxToParametersMap[idx]));
                    }
                }
            }

            // No dependencies between parameters detected - no need to process further the second evaluation
            if (parametersDependencies.Count == 0)
            {
                return;
            }

            DirectedGraph<EvalData> parametersDependenciesGraph = new(parametersDependencies);
            // Get the transitive closure of parameters that need to be recalculated, based on the knowledge of params that
            IReadOnlyList<EvalData> disabledParameters = parameters.Where(p => p.IsEnabledConditionResult.HasValue && !p.IsEnabledConditionResult.Value).ToList();
            DirectedGraph<EvalData> parametersToRecalculate =
                parametersDependenciesGraph.GetSubGraphDependandOnVertices(disabledParameters, includeSeedVertices: false);

            // Second traversal - for transitive dependencies of parameters that need to be disabled
            if (parametersToRecalculate.TryGetTopologicalSort(out IReadOnlyList<EvalData> orderedParameters))
            {
                disabledParameters.ForEach(p => variableCollection.Remove(p.ParameterDefinition.Name));

                if (parametersDependenciesGraph.HasCycle(out var cycle))
                {
                    var format = "Parameter conditions contain cyclic dependency: [{0}]. With current values of parameters it's possible to deterministically evaluate parameters - so proceeding further. However template should be reviewed as instantiation with different parameters can lead to error.";
                    logger.LogWarning(format, cycle.Select(p => p.ParameterDefinition.Name).ToCsvString());
                }

                foreach (EvalData parameter in orderedParameters)
                {
                    bool isEnabled = Cpp2StyleEvaluatorDefinition.EvaluateFromString(logger, parameter.ParameterDefinition.Precedence.IsEnabledCondition, variableCollection);
                    parameter.IsEnabledConditionResult = isEnabled;
                    if (!isEnabled)
                    {
                        variableCollection.Remove(parameter.ParameterDefinition.Name);
                    }
                }
            }
            else if (parametersToRecalculate.HasCycle(out var cycle))
            {
                throw new TemplateAuthoringException(
                    string.Format(
                        "Parameter conditions contain cyclic dependency: [{0}] that is preventing deterministic evaluation",
                        cycle.Select(p => p.ParameterDefinition.Name).ToCsvString()),
                    "Conditional Parameters");
            }
            else
            {
                throw new Exception("Unexpected internal error - unable to perform topological sort of parameter dependencies that do not appear to have a cyclic dependencies.");
            }
        }

        private void EvaluateRequirementCondition(
            IReadOnlyList<EvalData> parameters,
            VariableCollection variableCollection,
            ILogger logger
        )
        {
            foreach (EvalData parameter in parameters)
            {
                if (!string.IsNullOrEmpty(parameter.ParameterDefinition.Precedence.IsRequiredCondition))
                {
                    parameter.IsRequiredConditionResult =
                        Cpp2StyleEvaluatorDefinition.EvaluateFromString(logger, parameter.ParameterDefinition.Precedence.IsRequiredCondition, variableCollection);
                }
            }
        }

        private class EvalData
        {
            private object? _value;

            public EvalData(
                ITemplateParameter parameterDefinition,
                object? value,
                bool? isEnabledConditionResult,
                bool? isRequiredConditionResult)
            {
                ParameterDefinition = parameterDefinition;
                _value = value;
                IsEnabledConditionResult = isEnabledConditionResult;
                IsRequiredConditionResult = isRequiredConditionResult;
            }

            public EvalData(ITemplateParameter parameterDefinition)
            {
                ParameterDefinition = parameterDefinition;
            }

            public EvalData(EvaluatedParameterData other)
                : this(other.ParameterDefinition, other.Value, other.IsEnabledConditionResult, other.IsRequiredConditionResult)
            { }

            public ITemplateParameter ParameterDefinition { get; }

            public object? Value
            {
                get { return _value; }

                set
                {
                    _value = value;
                    InputDataState = (value == null) ? InputDataState.ExplicitNull : InputDataState.Set;
                }
            }

            public InputDataState InputDataState { get; private set; } = InputDataState.Unset;

            public bool? IsEnabledConditionResult { get; set; }

            public bool? IsRequiredConditionResult { get; set; }

            public override string ToString() => $"{ParameterDefinition}: {Value?.ToString() ?? "<null>"}";

            public EvaluatedParameterData ToParameterData()
            {
                return new EvaluatedParameterData(
                    this.ParameterDefinition,
                    this.Value,
                    this.IsEnabledConditionResult,
                    this.IsRequiredConditionResult,
                    InputDataState);
            }
        }
    }
}
