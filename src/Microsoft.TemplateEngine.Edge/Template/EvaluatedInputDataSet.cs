// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Parameters;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Edge.Template;

public class EvaluatedInputDataSet : InputDataSet, IEvaluatedInputDataSet
{
    public EvaluatedInputDataSet(IParametersDefinition parameters, IReadOnlyList<EvaluatedInputParameterData> parameterData)
        : base(parameters, parameterData/*FilterDefinitions(parameters.AsReadonlyDictionary(), parameterData)*/)
    {
        ParametersData = parameterData
            .ToDictionary(d => d.ParameterDefinition, d => d);
        this.CheckProperEvaluations();
    }

    public new IReadOnlyDictionary<ITemplateParameter, EvaluatedInputParameterData> ParametersData { get; }

    public bool ContinueOnMismatchedEvaluations { get; init; }

    private static IReadOnlyDictionary<string, ITemplateParameter> FilterDefinitions(
        IReadOnlyDictionary<string, ITemplateParameter> input, IEnumerable<EvaluatedInputParameterData> parameterData)
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
            ParametersData.Values.Where(p =>
                p.IsEnabledConditionResult == null ^
                string.IsNullOrEmpty(p.ParameterDefinition.Precedence.IsEnabledCondition)).ToList());

        ErrorOutOnMismatchedConditionEvaluation(
            ParametersData.Values.Where(p =>
                p.IsRequiredConditionResult == null ^
                string.IsNullOrEmpty(p.ParameterDefinition.Precedence.IsRequiredCondition)).ToList());
    }

    private void ErrorOutOnMismatchedConditionEvaluation(IReadOnlyList<EvaluatedInputParameterData> offendingParameters)
    {
        if (offendingParameters.Any())
        {
            throw new Exception(
                string.Format(LocalizableStrings.EvaluatedInputDataSet_Error_MismatchedConditions, string.Join(", ", offendingParameters)));
        }
    }
}
