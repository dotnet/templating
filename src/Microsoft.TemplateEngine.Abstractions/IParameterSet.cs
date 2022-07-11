// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

#pragma warning disable RS0016 // Add public types and members to the declared API
#pragma warning disable SA1507 // Code should not contain multiple blank lines in a row
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1516 // Elements should be separated by blank line

namespace Microsoft.TemplateEngine.Abstractions
{
    /// <summary>
    /// Defines a set of template parameters.
    /// </summary>
    public interface IParameterSet
    {
        /// <summary>
        /// Gets an enumerator iterating through the parameter definitions of the template.
        /// </summary>
        IEnumerable<ITemplateParameter> ParameterDefinitions { get; }

        /// <summary>
        /// Gets a collection of template parameters and their values.
        /// </summary>
        IDictionary<ITemplateParameter, object> ResolvedValues { get; }

        /// <summary>
        /// Gets a parameter definition with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Parameter name to get.</param>
        /// <param name="parameter">Retrieved parameter or null if the parameter is not found.</param>
        /// <returns>true if the parameter was retrieved, false otherwise.</returns>
        bool TryGetParameterDefinition(string name, out ITemplateParameter parameter);
    }

    public interface IParameterDefinitionsSet : IEnumerable<ITemplateParameter>, IReadOnlyDictionary<string, ITemplateParameter>
    {
        ///// <summary>
        ///// Gets an enumerator iterating through the parameter definitions of the template.
        ///// </summary>
        //IEnumerable<ITemplateParameter> ParameterDefinitions { get; }

        ///// <summary>
        ///// Gets a collection of template parameters and their values.
        ///// </summary>
        //IDictionary<ITemplateParameter, object> ResolvedValues { get; }

        ///// <summary>
        ///// Gets a parameter definition with the specified <paramref name="name"/>.
        ///// </summary>
        ///// <param name="name">Parameter name to get.</param>
        ///// <param name="parameter">Retrieved parameter or null if the parameter is not found.</param>
        ///// <returns>true if the parameter was retrieved, false otherwise.</returns>
        //bool TryGetParameterDefinition(string name, out ITemplateParameter parameter);
    }


    // param definitions
    // param values
    // evaluated param values

    public interface IParameterSet2
    {
        IParameterDefinitionsSet ParameterDefinitions { get; }
        //IReadOnlyDictionary<ITemplateParameter, object> ResolvedValues { get; }
    }

    public interface IParameterSetBuilder
    {
        void SetParameterValue(ITemplateParameter parameter, object value);

        bool HasParameterValue(ITemplateParameter parameter);

        IEvaluatedParameterSet EvaluateConditionalParameters(ILogger logger);
    }



    public interface IEvaluatedParameterSet : IParameterSet2
    {
        IReadOnlyDictionary<ITemplateParameter, EvaluatedParameterData> EvaluatedValues { get; }

        void RemoveDisabledParamsFromTemplate(ITemplate template);
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
    }

    public class EvaluatedParameterData : ParameterData
    {
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

        // TODO: do those need to be exposed? (probably yes - if both are set)
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
}
