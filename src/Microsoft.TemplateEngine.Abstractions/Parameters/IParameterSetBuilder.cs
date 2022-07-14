// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Logging;

namespace Microsoft.TemplateEngine.Abstractions.Parameters;

public interface IParameterSetBuilder : IParametersDefinition
{
    void SetParameterValue(ITemplateParameter parameter, object value);

    void SetParameterEvaluation(ITemplateParameter parameter, EvaluatedParameterData evaluatedParameterData);

    bool HasParameterValue(ITemplateParameter parameter);

    void EvaluateConditionalParameters(ILogger logger);

    IEvaluatedParameterSetData Build();
}
