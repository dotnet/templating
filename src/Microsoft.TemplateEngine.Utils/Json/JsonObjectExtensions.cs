using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Utils.Json
{
    public static class JsonObjectExtensions
    {
        public static ISet<string> ExtractValues(this IJsonObject obj, params (string propertyName, Action<IJsonToken> valueExtractor)[] mappings)
        {
            return obj.ExtractValues(mappings.ToDictionary(x => x.propertyName, x => x.valueExtractor, StringComparer.OrdinalIgnoreCase));
        }

        public static ISet<string> ExtractValues(this IJsonObject obj, IEnumerable<(string propertyName, Action<IJsonToken> valueExtractor)> mappings)
        {
            return obj.ExtractValues(mappings.ToDictionary(x => x.propertyName, x => x.valueExtractor, StringComparer.OrdinalIgnoreCase));
        }
    }
}
