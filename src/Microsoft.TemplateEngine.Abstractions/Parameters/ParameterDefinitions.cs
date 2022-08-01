// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TemplateEngine.Abstractions.Parameters
{
    public class ParameterDefinitions : IParameterDefinitionSet
    {
        public static readonly IParameterDefinitionSet Empty = new ParameterDefinitions((IEnumerable<ITemplateParameter>?)null);

        private readonly IReadOnlyDictionary<string, ITemplateParameter> _parameters;

        public ParameterDefinitions(IReadOnlyDictionary<string, ITemplateParameter>? parameters) =>
            _parameters = parameters ?? new Dictionary<string, ITemplateParameter>();

        public ParameterDefinitions(IEnumerable<ITemplateParameter>? parameters)
            : this(parameters?.ToDictionary(p => p.Name, p => p))
        { }

        public ParameterDefinitions(IParameterDefinitionSet other) : this(other.AsReadonlyDictionary())
        { }

        public IEnumerable<string> Keys => _parameters.Keys;

        public IEnumerable<ITemplateParameter> Values => _parameters.Values;

        public int Count => _parameters.Count;

        public ITemplateParameter this[string key] => _parameters[key];

        public ITemplateParameter this[int index] => _parameters.Values.ElementAt(index);

        public IReadOnlyDictionary<string, ITemplateParameter> AsReadonlyDictionary() => _parameters;

        public bool ContainsKey(string key) => _parameters.ContainsKey(key);

        public IEnumerator<ITemplateParameter> GetEnumerator() => _parameters.Values.GetEnumerator();

        public bool TryGetValue(string key, out ITemplateParameter value) => _parameters.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => _parameters.Values.GetEnumerator();
    }
}
