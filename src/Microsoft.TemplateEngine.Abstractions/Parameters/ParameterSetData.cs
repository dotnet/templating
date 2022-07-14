// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.TemplateEngine.Abstractions.Parameters;

public class ParameterSetData : ParametersDefinition, IParameterSetData
{
    public ParameterSetData(IParametersDefinition parameters, IReadOnlyList<ParameterData> parameterData)
        : base(parameters.AsReadonlyDictionary())
    {
        ParametersData = parameterData.ToDictionary(d => d.ParameterDefinition, d => d);
    }

#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
    public ParameterSetData(ITemplateInfo templateInfo, IReadOnlyDictionary<string, string?>? inputParameters = null)
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
        : base(templateInfo.Parameters)
    {
        ParametersData = templateInfo.Parameters.ToDictionary(p => p, p =>
        {
            string? value = null;
            bool isSet = inputParameters != null && inputParameters.TryGetValue(p.Name, out value);
            return new ParameterData(p, value, isSet ? (value == null ? InputDataState.ExplicitNull : InputDataState.Set) : InputDataState.Unset);
        });
    }

    public IReadOnlyDictionary<ITemplateParameter, ParameterData> ParametersData { get; }
}
