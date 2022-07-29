// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Parameters;

namespace Microsoft.TemplateEngine.Edge.Template;

internal interface IParameterSetBuilder : IParametersDefinition
{
    void SetParameterValue(ITemplateParameter parameter, object value, DataSource dataSource);

    void SetParameterEvaluation(ITemplateParameter parameter, EvaluatedInputParameterData evaluatedParameterData);

    bool HasParameterValue(ITemplateParameter parameter);

    void EvaluateConditionalParameters(IGenerator generator, ILogger logger);

    bool CheckIsParametersEvaluationCorrect(IGenerator generator, ILogger logger, out IReadOnlyList<string> paramsWithInvalidEvaluations);

    InputDataSet Build();
}
