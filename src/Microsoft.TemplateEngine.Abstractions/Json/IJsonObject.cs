using System;
using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions.Json
{
    public interface IJsonObject : IJsonToken
    {
        IEnumerable<string> PropertyNames { get; }

        IJsonObject RemoveValue(string propertyName);

        IJsonObject SetValue(string propertyName, IJsonToken value);

        ISet<string> ExtractValues(IReadOnlyDictionary<string, Action<IJsonToken>> mappings);

        ISet<string> ExtractValues<T>(T context, IReadOnlyDictionary<string, Action<IJsonToken, T>> mappings);

        IEnumerable<KeyValuePair<string, IJsonToken>> Properties();
    }
}
