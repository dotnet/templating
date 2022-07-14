// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions.Parameters;

public interface IParameterSetData : IParametersDefinition
{
    /// <summary>
    /// Data for enabled parameters.
    /// </summary>
    IReadOnlyDictionary<ITemplateParameter, ParameterData> ParametersData { get; }
}
