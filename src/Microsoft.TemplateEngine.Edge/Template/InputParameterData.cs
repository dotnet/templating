// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        VerifyInputState();
    }

    public ITemplateParameter ParameterDefinition { get; }

    public object? Value { get; }

    public DataSource DataSource { get; }

    public InputDataState InputDataState { get; }

    public override string ToString() => $"{ParameterDefinition}: {Value?.ToString() ?? "<null>"}";

    private void VerifyInputState()
    {
        if (InputDataState == InputDataState.Unset)
        {
            if (Value != null)
            {
                throw new ArgumentException(
                    string.Format(
                    "It's disallowed to pass an input data value (even empty string) when it's taged as InputDataState.Unset. Param: {0}",
                    ParameterDefinition.Name));
            }
        }
        else if (InputDataStateUtil.GetInputDataState(Value) != InputDataState)
        {
            throw new ArgumentException(
                string.Format(
                    "Param {0} has disallowed combination of input data value ({1}) and InputDataState ({2}).",
                    ParameterDefinition.Name,
                    Value,
                    InputDataState));
        }
    }
}
