// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.TemplateEngine.Abstractions.Parameters;

/// <inheritdoc/>
public class ParameterSetData : IParameterSetData
{
    private readonly IReadOnlyDictionary<ITemplateParameter, ParameterData> _parametersData;

    /// <summary>
    /// Creates new instance of the <see cref="ParameterSetData"/> data type.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="parameterData"></param>
    public ParameterSetData(IParametersDefinition parameters, IReadOnlyList<ParameterData> parameterData)
    {
        ParametersDefinition = new ParametersDefinition(parameters.AsReadonlyDictionary());
        _parametersData = parameterData.ToDictionary(d => d.ParameterDefinition, d => d);
    }

#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
    /// <summary>
    /// Creates new instance of the <see cref="ParameterSetData"/> data type.
    /// To be used for compatibility purposes in places where old dictionary parameter set was used.
    /// </summary>
    /// <param name="templateInfo"></param>
    /// <param name="inputParameters"></param>
    public ParameterSetData(ITemplateInfo templateInfo, IReadOnlyDictionary<string, string?>? inputParameters = null)
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
        : this(templateInfo, inputParameters?.ToDictionary(p => p.Key, p => (object?)p.Value))
    { }

    /// <summary>
    /// Creates new instance of the <see cref="ParameterSetData"/> data type.
    /// To be used for compatibility purposes in places where old dictionary parameter set was used.
    /// </summary>
    /// <param name="templateInfo"></param>
    /// <param name="inputParameters"></param>
    public ParameterSetData(ITemplateInfo templateInfo, IReadOnlyDictionary<string, object?>? inputParameters)
    {
        ParametersDefinition = new ParametersDefinition(templateInfo.ParametersDefinition);
        _parametersData = templateInfo.ParametersDefinition.ToDictionary(p => p, p =>
        {
            object? value = null;
            bool isSet = inputParameters != null && inputParameters.TryGetValue(p.Name, out value);
            return new ParameterData(p, value, isSet ? DataSource.Host : DataSource.NoSource);
        });
    }

    /// <inheritdoc/>
    public IParametersDefinition ParametersDefinition { get; }

    /// <inheritdoc/>
    public int Count => _parametersData.Count;

    /// <inheritdoc/>
    public IEnumerable<ITemplateParameter> Keys => _parametersData.Keys;

    /// <inheritdoc/>
    public IEnumerable<ParameterData> Values => _parametersData.Values;

    /// <inheritdoc/>
    public ParameterData this[ITemplateParameter key] => _parametersData[key];

    /// <summary>
    /// Creates instance of <see cref="IParameterSetData"/> from the legacy <see cref="IParameterSet"/>.
    /// </summary>
    /// <param name="parameterSet">Legacy parameterset to be converted.</param>
    /// <returns></returns>
    [Obsolete("IParameterSet should not be used - it is replaced with IParameterSetData", false)]
    public static IParameterSetData FromLegacyParameterSet(IParameterSet parameterSet)
    {
        IParametersDefinition parametersDefinition = new ParametersDefinition(parameterSet.ParameterDefinitions);
        IReadOnlyList<ParameterData> data = parameterSet.ResolvedValues.Select(p =>
                new ParameterData(p.Key, p.Value, DataSource.Host))
            .ToList();
        return new ParameterSetData(parametersDefinition, data);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<ITemplateParameter, ParameterData>> GetEnumerator() => _parametersData.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public bool ContainsKey(ITemplateParameter key) => _parametersData.ContainsKey(key);

    /// <inheritdoc/>
    public bool TryGetValue(ITemplateParameter key, out ParameterData value) => _parametersData.TryGetValue(key, out value);
}
