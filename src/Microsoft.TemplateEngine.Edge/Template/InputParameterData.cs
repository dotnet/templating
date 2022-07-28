// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Parameters;

namespace Microsoft.TemplateEngine.Edge.Template;

public class InputParameterData
{
    public InputParameterData(
        ITemplateParameter parameterDefinition,
        object? value,
        DataSource dataSource,
        InputDataState inputDataState = InputDataState.Set)
    {
        ParameterDefinition = parameterDefinition;
        Value = value;
        DataSource = dataSource;
        InputDataState = inputDataState;
    }

    public ITemplateParameter ParameterDefinition { get; }

    public object? Value { get; }

    public DataSource DataSource { get; }

    public InputDataState InputDataState { get; }

    public override string ToString() => $"{ParameterDefinition}: {Value?.ToString() ?? "<null>"}";
}
