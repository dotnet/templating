// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Parameters;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Edge.Template;

public class InputDataSet : ParametersDefinition
{
    public InputDataSet(IParametersDefinition parameters, IReadOnlyList<InputParameterData> parameterData)
        : base(parameters.AsReadonlyDictionary())
    {
        ParametersData = parameterData.ToDictionary(d => d.ParameterDefinition, d => d);
    }

#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
    public InputDataSet(ITemplateInfo templateInfo, IReadOnlyDictionary<string, string?>? inputParameters = null)
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
        : this(templateInfo, inputParameters?.ToDictionary(p => p.Key, p => (object?)p.Value))
    { }

    private InputDataSet(ITemplateInfo templateInfo, IReadOnlyDictionary<string, object?>? inputParameters)
        : base(templateInfo.ParametersDefinition)
    {
        ParametersData = templateInfo.ParametersDefinition.ToDictionary(p => p, p =>
        {
            object? value = null;
            bool isSet = inputParameters != null && inputParameters.TryGetValue(p.Name, out value);
            return new InputParameterData(p, value, isSet ? DataSource.Host : DataSource.NoSource, isSet ? InputDataStateUtil.GetInputDataState(value) : InputDataState.Unset);
        });
    }

    public bool ContinueOnMismatchedConditionsEvaluation { get; init; }

    public IReadOnlyDictionary<ITemplateParameter, InputParameterData> ParametersData { get; }
}
