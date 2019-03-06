using System;
using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions.Json
{
    public interface IJsonObject : IJsonToken
    {
        IEnumerable<string> PropertyNames { get; }

        IJsonObject RemoveValue(string propertyName);

        IJsonObject SetValue(string propertyName, IJsonToken value);

        IReadOnlyCollection<string> ExtractValues(params (string propertyName, Action<IJsonToken> valueExtractor)[] mappings);
    }
}
