// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.TemplateEngine.Abstractions.Parameters;

public class ParameterData
{
    public ParameterData(
        ITemplateParameter parameterDefinition,
        object? value,
        InputDataState inputDataState = InputDataState.Set)
    {
        ParameterDefinition = parameterDefinition;
        Value = value;
        InputDataState = inputDataState;
    }

    public ITemplateParameter ParameterDefinition { get; }

    public object? Value { get; }

    public InputDataState InputDataState { get; }

    public override string ToString() => $"{ParameterDefinition}: {Value?.ToString() ?? "<null>"}";
}
