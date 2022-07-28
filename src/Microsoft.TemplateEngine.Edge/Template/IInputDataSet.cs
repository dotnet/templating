// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Parameters;

namespace Microsoft.TemplateEngine.Edge.Template;

public interface IInputDataSet<TData> : IParametersDefinition where TData : InputParameterData
{
    /// <summary>
    /// Parameters data.
    /// </summary>
    IReadOnlyDictionary<ITemplateParameter, TData> ParametersData { get; }

    /// <summary>
    /// Converts this instance to <see cref="IParameterSetData"/> object model.
    /// </summary>
    /// <returns></returns>
    IParameterSetData ToParameterSetData();
}

public interface IInputDataSet : IInputDataSet<InputParameterData>
{ }
