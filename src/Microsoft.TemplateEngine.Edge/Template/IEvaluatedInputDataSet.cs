// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Parameters;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Edge.Template;

public interface IEvaluatedInputDataSet : IInputDataSet<EvaluatedInputParameterData>
{
    /// <summary>
    /// Indicates whether template creator should ignore evaluation results that does not match validation evaluation.
    ///  Warning will be logged and external evaluation results will be used.
    /// </summary>
    bool ContinueOnMismatchedEvaluations { get; }
}
