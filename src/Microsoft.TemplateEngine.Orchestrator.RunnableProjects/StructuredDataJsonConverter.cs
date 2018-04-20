using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.TemplateEngine.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    public class StructuredDataJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IStructuredData).GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    {
                        Dictionary<string, IStructuredData> result = new Dictionary<string, IStructuredData>(StringComparer.Ordinal);
                        while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                        {
                            string propertyName = (string)reader.Value;
                            if (reader.Read())
                            {
                                StructuredData value = (StructuredData)serializer.Deserialize(reader, typeof(StructuredData));
                                result[propertyName] = value;
                            }
                        }
                        return new StructuredData(result);
                    }
                case JsonToken.StartArray:
                    {
                        List<IStructuredData> result = new List<IStructuredData>();
                        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                        {
                            StructuredData value = (StructuredData)serializer.Deserialize(reader, typeof(StructuredData));
                            result.Add(value);
                        }
                        return new StructuredData(result);
                    }
                default:
                    return new StructuredData(reader.Value);
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            IStructuredData d = (IStructuredData)value;

            if (d.IsPrimitive)
            {
                serializer.Serialize(writer, d.Value);
                return;
            }

            if (d.IsArrayData)
            {
                List<IStructuredData> values = new List<IStructuredData>();
                for (int i = 0; i < d.Count; ++i)
                {
                    if (d.TryGetValueByIndex(i, out IStructuredData tmp))
                    {
                        values.Add(tmp);
                    }
                }

                serializer.Serialize(writer, values);
                return;
            }

            if (d.IsObjectData)
            {
                Dictionary<string, IStructuredData> values = new Dictionary<string, IStructuredData>(StringComparer.Ordinal);
                foreach (string key in d.Keys)
                {
                    if (d.TryGetNamedValue(key, out IStructuredData tmp))
                    {
                        values[key] = tmp;
                    }
                }
            }
        }
    }
}
