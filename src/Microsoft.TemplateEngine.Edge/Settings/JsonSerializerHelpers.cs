using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    // TODO: Move this out of edge.
    internal static class JsonSerializerHelpers
    {
        public static bool TrySerializeStringDictionary(IReadOnlyDictionary<string, string> toSerialize, out JObject serialized)
        {
            try
            {
                serialized = new JObject();

                foreach (KeyValuePair<string, string> entry in toSerialize)
                {
                    serialized.Add(entry.Key, entry.Value);
                }

                return true;
            }
            catch
            {
                serialized = null;
                return false;
            }
        }

        public static Func<string, string> StringKeyConverter = input => input;
        public static Func<Guid, string> GuidKeyConverter = input => input.ToString();

        public static Func<string, JToken> StringValueConverter = input => { return new JValue(input); };
        public static Func<Guid, JToken> GuidValueConverter = input => { return new JValue(input); };

        public static bool TrySerializeDictionary<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> toSerialize, Func<TKey, string> keyConverter, Func<TValue, JToken> valueConverter, out JObject serialized)
        {
            try
            {
                serialized = new JObject();

                foreach (KeyValuePair<TKey, TValue> entry in toSerialize)
                {
                    string key = keyConverter(entry.Key);
                    JToken value = valueConverter(entry.Value);
                    serialized.Add(key, value);
                }

                return true;
            }
            catch
            {
                serialized = null;
                return false;
            }
        }

        public static bool TrySerializeIEnumerable<T>(IEnumerable<T> toSerialize, Func<T, JToken> elementConverter, out JArray serialized)
        {
            try
            {
                serialized = new JArray();
                foreach (T element in toSerialize)
                {
                    serialized.Add(elementConverter(element));
                }

                return true;
            }
            catch
            {
                serialized = null;
                return false;
            }
        }
    }
}
