// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
        : this(templateInfo, inputParameters?.ToDictionary(p => p.Key, p => (object?)p.Value))
    { }

    public ParameterSetData(ITemplateInfo templateInfo, IReadOnlyDictionary<string, object?>? inputParameters)
        : base(templateInfo.ParametersDefinition)
    {
        ParametersData = templateInfo.ParametersDefinition.ToDictionary(p => p, p =>
        {
            object? value = null;
            bool isSet = inputParameters != null && inputParameters.TryGetValue(p.Name, out value);
            return new ParameterData(p, value, isSet ? DataSource.Host : DataSource.NoSource);
        });
    }

    public IReadOnlyDictionary<ITemplateParameter, ParameterData> ParametersData { get; }

    [Obsolete("IParameterSet should not be used - it is replaced with IParameterSetData", false)]
    public static IParameterSetData FromLegacyParameterSet(IParameterSet parameterSet)
    {
        IParametersDefinition parametersDefinition = new ParametersDefinition(parameterSet.ParameterDefinitions);
        IReadOnlyList<ParameterData> data = parameterSet.ResolvedValues.Select(p =>
            new ParameterData(p.Key, p.Value, DataSource.Host))
            .ToList();
        return new ParameterSetData(parametersDefinition, data);
    }
}
