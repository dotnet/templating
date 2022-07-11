// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

#pragma warning disable RS0016 // Add public types and members to the declared API
#pragma warning disable SA1507 // Code should not contain multiple blank lines in a row
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1516 // Elements should be separated by blank line
#pragma warning disable SA1202 // Elements should be ordered by access

namespace Microsoft.TemplateEngine.Abstractions
{
    public interface IParameterDefinitionsSet : IEnumerable<ITemplateParameter>, IReadOnlyDictionary<string, ITemplateParameter>
    {
        /// <summary>
        /// Casts the Parameter set to unambiguous enumerable.
        /// </summary>
        /// <returns></returns>
        IEnumerable<ITemplateParameter> AsEnumerable();
    }

    /// <summary>
    /// Defines a set of template parameters.
    /// </summary>
    public interface IParameterSet2
    {
        /// <summary>
        /// Gets an enumerator iterating through the parameter definitions of the template.
        /// </summary>
        IParameterDefinitionsSet ParameterDefinitions { get; }
    }

    public interface IParameterSetBuilder : IParameterSet2
    {
        void SetParameterValue(ITemplateParameter parameter, object value);

        void SetParameterEvaluation(ITemplateParameter parameter, EvaluatedParameterData evaluatedParameterData);

        bool HasParameterValue(ITemplateParameter parameter);

        void EvaluateConditionalParameters(ILogger logger);

        IEvaluatedParameterSetData Build();
    }

    public interface IParameterSetData : IParameterSet2
    {
        /// <summary>
        /// Data for enabled parameters.
        /// </summary>
        IReadOnlyDictionary<ITemplateParameter, ParameterData> ParametersData { get; }
    }

    public interface IEvaluatedParameterSetData : IParameterSetData
    {
        /// <summary>
        /// Data for all evaluated parameters - including disabled ones.
        /// </summary>
        IReadOnlyDictionary<ITemplateParameter, EvaluatedParameterData> AllParametersData { get; }
    }

    internal class ParameterDefinitionsSet : IParameterDefinitionsSet
    {
        private readonly IReadOnlyDictionary<string, ITemplateParameter> _parameters;

        public ParameterDefinitionsSet(IReadOnlyDictionary<string, ITemplateParameter> parameters) => _parameters = parameters;

        public IEnumerable<string> Keys => _parameters.Keys;

        public IEnumerable<ITemplateParameter> Values => _parameters.Values;

        public int Count => _parameters.Count;

        public ITemplateParameter this[string key] => _parameters[key];

        public IEnumerable<ITemplateParameter> AsEnumerable() => this;

        public bool ContainsKey(string key) => _parameters.ContainsKey(key);

        public IEnumerator<ITemplateParameter> GetEnumerator() => _parameters.Values.GetEnumerator();

        public bool TryGetValue(string key, out ITemplateParameter value) => _parameters.TryGetValue(key, out value);

        IEnumerator<KeyValuePair<string, ITemplateParameter>> IEnumerable<KeyValuePair<string, ITemplateParameter>>.GetEnumerator() => _parameters.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _parameters.GetEnumerator();
    }


    public class ParameterSet2 : IParameterSet2
    {
        public ParameterSet2(IReadOnlyDictionary<string, ITemplateParameter> parameters) => ParameterDefinitions = new ParameterDefinitionsSet(parameters);

        public IParameterDefinitionsSet ParameterDefinitions { get; }
    }

    public class ParameterSetData : ParameterSet2, IParameterSetData
    {
        public ParameterSetData(IParameterDefinitionsSet parameters, IReadOnlyList<ParameterData> parameterData)
            : base(parameters)
        {
            ParametersData = parameterData.ToDictionary(d => d.ParameterDefinition, d => d);
        }

#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
        public ParameterSetData(ITemplateInfo templateInfo, IReadOnlyDictionary<string, string?>? inputParameters = null)
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
            : base(templateInfo.Parameters.ToDictionary(p => p.Name, p => p))
        {
            ParametersData = templateInfo.Parameters.ToDictionary(p => p, p =>
            {
                string? value = null;
                bool isSet = inputParameters != null && inputParameters.TryGetValue(p.Name, out value);
                return new ParameterData(p, value, isSet ? (value == null ? InputDataState.ExplicitNull : InputDataState.Set) : InputDataState.Unset);
            });
        }

        public IReadOnlyDictionary<ITemplateParameter, ParameterData> ParametersData { get; }
    }

    public class EvaluatedParameterSetData : ParameterSet2, IEvaluatedParameterSetData
    {
        public EvaluatedParameterSetData(IParameterDefinitionsSet parameters, IReadOnlyList<EvaluatedParameterData> parameterData)
            : base(FilterDefinitions(parameters, parameterData))
        {
            AllParametersData = parameterData.ToDictionary(d => d.ParameterDefinition, d => d);
            ParametersData = AllParametersData
                .Where(pair => pair.Value.EvaluatedPrecedence != EvaluatedPrecedence.Disabled)
                .ToDictionary(pair => pair.Key, pair => (ParameterData)pair.Value);
            this.CheckProperEvaluations();
        }

        public IReadOnlyDictionary<ITemplateParameter, EvaluatedParameterData> AllParametersData { get; }

        public IReadOnlyDictionary<ITemplateParameter, ParameterData> ParametersData { get; }

        private static IReadOnlyDictionary<string, ITemplateParameter> FilterDefinitions(
            IReadOnlyDictionary<string, ITemplateParameter> input, IEnumerable<EvaluatedParameterData> parameterData)
        {
            HashSet<ITemplateParameter> disabledParams = new HashSet<ITemplateParameter>(parameterData
                .Where(p => p.IsEnabledConditionResult.HasValue && !p.IsEnabledConditionResult.Value)
                .Select(p => p.ParameterDefinition));

            return input.Where(pair => !disabledParams.Contains(pair.Value))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private void CheckProperEvaluations()
        {
            ErrorOutOnMismatchedConditionEvaluation(
                AllParametersData.Values.Where(p =>
                    !(p.IsEnabledConditionResult == null ^
                    string.IsNullOrEmpty(p.ParameterDefinition.Precedence.IsEnabledCondition))).ToList());

            ErrorOutOnMismatchedConditionEvaluation(
                AllParametersData.Values.Where(p =>
                    !(p.IsRequiredConditionResult == null ^
                    string.IsNullOrEmpty(p.ParameterDefinition.Precedence.IsRequiredCondition))).ToList());
        }

        private void ErrorOutOnMismatchedConditionEvaluation(IReadOnlyList<EvaluatedParameterData> offendingParameters)
        {
            if (offendingParameters.Any())
            {
                //TODO: localize
                string format =
                    "Attempt to pass result of external evaluation of parameters conditions for parameter(s) that do not have appropriate condition set in template (IsEnabled or IsRequired attributes not populated with condition) or a failure to pass the condition results for parameters with condition(s) in template. Offending parameters: {0}";
                throw new Exception(
                    string.Format(format, string.Join(", ", offendingParameters)));
            }
        }
    }

    public enum InputDataState
    {
        Set,
        Unset,
        ExplicitNull
    }

    public class ParameterData
    {
        public ParameterData(
            ITemplateParameter parameterDefinition,
            object? value,
            InputDataState inputDataState = InputDataState.Set)
        {
            ParameterDefinition = parameterDefinition;
            Value = value;
            InputDataState = inputDataState;
        }

        public ITemplateParameter ParameterDefinition { get; }

        public object? Value { get; }

        public InputDataState InputDataState { get; }

        public override string ToString() => $"{ParameterDefinition}: {Value?.ToString() ?? "<null>"}";
    }

    public class EvaluatedParameterData : ParameterData
    {
        /// <summary>
        /// Constructor for <see cref="EvaluatedParameterData"/> type, that allows specification of results of external evaluation of conditions.
        /// </summary>
        /// <param name="parameterDefinition"></param>
        /// <param name="value">A stringified value of parameter or null for explicit unset. It's possible to indicate missing of parameter on input via <see cref="InputDataState"/> argument.</param>
        /// <param name="isEnabledConditionResult"></param>
        /// <param name="isRequiredConditionResult"></param>
        /// <param name="inputDataState">
        /// InputDataState.Unset indicates a situation that parameter was not specified on input (distinct situation from explicit null).
        ///  This would normally be achieved by not passing the parameter at all into the <see cref="EvaluatedParameterSetData"/>, however then it would not be possible
        ///  to specify the results of conditions calculations.
        /// </param>
        public EvaluatedParameterData(
            ITemplateParameter parameterDefinition,
            object? value,
            bool? isEnabledConditionResult,
            bool? isRequiredConditionResult,
            InputDataState inputDataState = InputDataState.Set)
        : base(parameterDefinition, value, inputDataState)
        {
            IsEnabledConditionResult = isEnabledConditionResult;
            IsRequiredConditionResult = isRequiredConditionResult;
            VerifyConditions();
            EvaluatedPrecedence = GetEvaluatedPrecedence();
        }

        public EvaluatedParameterData(EvaluatedParameterData old, object? newData)
        : base(old.ParameterDefinition, newData, newData == null ? InputDataState.ExplicitNull : InputDataState.Set)
        {
            IsEnabledConditionResult = old.IsEnabledConditionResult;
            IsRequiredConditionResult = old.IsRequiredConditionResult;
            EvaluatedPrecedence = old.EvaluatedPrecedence;
        }

        public bool? IsEnabledConditionResult { get; }

        public bool? IsRequiredConditionResult { get; }

        public EvaluatedPrecedence EvaluatedPrecedence { get; }

        private void VerifyConditions()
        {
            if (
                string.IsNullOrEmpty(ParameterDefinition.Precedence.IsEnabledCondition) ^ !IsEnabledConditionResult.HasValue
                ||
                string.IsNullOrEmpty(ParameterDefinition.Precedence.IsRequiredCondition) ^ !IsRequiredConditionResult.HasValue
            )
            {
                // TODO: localize
                throw new ArgumentException("Passed parameters conditions results mismatch");
            }
        }

        private EvaluatedPrecedence GetEvaluatedPrecedence()
        {
            switch (ParameterDefinition.Precedence.PrecedenceDefinition)
            {
                case PrecedenceDefinition.Required:
                    return EvaluatedPrecedence.Required;
                // Conditionally required state is only set if enabled condition is not  present
                case PrecedenceDefinition.ConditionalyRequired:
                    return IsRequiredConditionResult!.Value ? EvaluatedPrecedence.Required : EvaluatedPrecedence.Optional;
                case PrecedenceDefinition.Optional:
                    return EvaluatedPrecedence.Optional;
                case PrecedenceDefinition.Implicit:
                    return EvaluatedPrecedence.Implicit;
                case PrecedenceDefinition.ConditionalyDisabled:
                    return !IsEnabledConditionResult!.Value
                        ? EvaluatedPrecedence.Disabled
                        :
                        IsRequiredConditionResult.HasValue && IsRequiredConditionResult.Value
                            ? EvaluatedPrecedence.Required : EvaluatedPrecedence.Optional;
                case PrecedenceDefinition.Disabled:
                    return EvaluatedPrecedence.Disabled;
                default:
                    throw new ArgumentOutOfRangeException("PrecedenceDefinition");
            }
        }
    }

#pragma warning restore RS0016 // Add public types and members to the declared API
#pragma warning restore SA1507 // Code should not contain multiple blank lines in a row
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1201 // Elements should appear in the correct order
#pragma warning restore SA1516 // Elements should be separated by blank line
#pragma warning restore SA1202 // Elements should be ordered by access
}
