// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions.Parameters;

public interface IEvaluatedParameterSetData : IParameterSetData
{
    /// <summary>
    /// Data for all parameters disabled after evaluation (complement to <see cref="IParameterSetData.ParametersData"/>).
    /// </summary>
    IReadOnlyDictionary<ITemplateParameter, EvaluatedParameterData> EvaluatedParametersData { get; }
}
