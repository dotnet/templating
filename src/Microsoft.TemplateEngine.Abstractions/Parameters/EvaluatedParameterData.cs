// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.TemplateEngine.Abstractions.Parameters;

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
            string format =
                "Attempt to pass result of external evaluation of parameters conditions for parameter(s) that do not have appropriate condition set in template (IsEnabled or IsRequired attributes not populated with condition) or a failure to pass the condition results for parameters with condition(s) in template. Offending parameter(s): {0}";

            throw new ArgumentException(string.Format(format, this.ParameterDefinition.Name));
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
                    IsRequiredConditionResult.HasValue && IsRequiredConditionResult.Value || ParameterDefinition.Precedence.IsRequired
                        ? EvaluatedPrecedence.Required : EvaluatedPrecedence.Optional;
            case PrecedenceDefinition.Disabled:
                return EvaluatedPrecedence.Disabled;
            default:
                throw new ArgumentOutOfRangeException("PrecedenceDefinition");
        }
    }
}
