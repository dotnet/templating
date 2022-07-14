// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.TemplateEngine.Abstractions.Parameters;

public class EvaluatedParameterSetData : ParametersDefinition, IEvaluatedParameterSetData
{
    public EvaluatedParameterSetData(IParametersDefinition parameters, IReadOnlyList<EvaluatedParameterData> parameterData)
        : base(FilterDefinitions(parameters.AsReadonlyDictionary(), parameterData))
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
                p.IsEnabledConditionResult == null ^
                string.IsNullOrEmpty(p.ParameterDefinition.Precedence.IsEnabledCondition)).ToList());

        ErrorOutOnMismatchedConditionEvaluation(
            AllParametersData.Values.Where(p =>
                p.IsRequiredConditionResult == null ^
                string.IsNullOrEmpty(p.ParameterDefinition.Precedence.IsRequiredCondition)).ToList());
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
