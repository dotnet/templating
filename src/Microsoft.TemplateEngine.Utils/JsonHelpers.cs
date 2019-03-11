using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Utils
{
    public static class JsonHelpers
    {
        public static (string, Action<IJsonToken>) CreateArrayValueExtractor<TType>(string key, List<TType> target)
        {
            return (key,
                (token) =>
                {
                    if (token is IJsonArray tokenArray)
                    {
                        foreach (IJsonValue element in tokenArray)
                        {
                            target.Add((TType)element.Value);
                        }
                    }
                }
            );
        }

        public static (string, Action<IJsonToken>)[] CreateStringKeyDictionaryExtractor<TValue>(IJsonObject token, IDictionary<string, TValue> target)
        {
            List<string> propertyNames = token.PropertyNames.ToList();
            (string, Action<IJsonToken>)[] extractorArray = new (string, Action<IJsonToken>)[propertyNames.Count];

            for (int i = 0; i < propertyNames.Count; ++i)
            {
                string key = propertyNames[i];

                extractorArray[i] = (key,
                    (element) =>
                    {
                        if (element is IJsonValue elementValue)
                        {
                            target[key] = (TValue)elementValue.Value;
                        }
                    }
                );
            }

            return extractorArray;
        }
    }
}
