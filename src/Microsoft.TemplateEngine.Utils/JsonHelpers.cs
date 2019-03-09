using System;
using System.Collections.Generic;
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
            List<(string, Action<IJsonToken>)> extractorList = new List<(string, Action<IJsonToken>)>();

            foreach (string key in token.PropertyNames)
            {
                extractorList.Add((key,
                    (element) =>
                    {
                        if (element is IJsonValue elementValue)
                        {
                            target[key] = (TValue)elementValue.Value;
                        }
                    }
                ));
            }

            return extractorList.ToArray();
        }
    }
}
