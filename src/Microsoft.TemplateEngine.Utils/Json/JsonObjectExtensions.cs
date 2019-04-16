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

        public static ISet<string> ExtractValues<T>(this IJsonObject obj, string selectorPropertyName, IReadOnlyDictionary<string, IJsonDeserializationBuilder<T>> mappingsSelector, IJsonDeserializationBuilder<T> defaultMapping, out T result)
        {
            result = default;
            Dictionary<string, IJsonToken> stash = null;
            ISet<string> foundPropertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            using (IEnumerator<KeyValuePair<string, IJsonToken>> enumerator = obj.Properties().GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    KeyValuePair<string, IJsonToken> entry = enumerator.Current;
                    if (string.Equals(entry.Key, selectorPropertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!(entry.Value is IJsonValue value))
                        {
                            return foundPropertyNames;
                        }

                        if (!mappingsSelector.TryGetValue(value.Value?.ToString() ?? string.Empty, out IJsonDeserializationBuilder<T> handler))
                        {
                            if (defaultMapping is null)
                            {
                                return foundPropertyNames;
                            }
                            else
                            {
                                handler = defaultMapping;
                            }
                        }

                        foundPropertyNames.Add(entry.Key);
                        result = handler.Deserialize(stash, enumerator);
                    }
                    else
                    {
                        if (stash is null)
                        {
                            stash = new Dictionary<string, IJsonToken>(StringComparer.OrdinalIgnoreCase) { { entry.Key, entry.Value } };
                        }
                        else
                        {
                            stash[entry.Key] = entry.Value;
                        }
                    }
                }
            }

            return foundPropertyNames;
        }
    }
}
