﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.TemplateEngine.Abstractions.Parameters;

public class ParameterData
{
    public ParameterData(
        ITemplateParameter parameterDefinition,
        object? value,
        DataSource source,
        bool isEnabled = true)
    {
        ParameterDefinition = parameterDefinition;
        Value = value;
        DataSource = source;
        IsEnabled = isEnabled;
    }

    public ITemplateParameter ParameterDefinition { get; }

    public object? Value { get; }

    public DataSource DataSource { get; }

    public bool IsEnabled { get; }

    public override string ToString() => $"{ParameterDefinition}: {Value?.ToString() ?? "<null>"}";
}
