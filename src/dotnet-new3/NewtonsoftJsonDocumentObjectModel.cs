//#if NET45
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.TemplateEngine.Abstractions.Json;
using Newtonsoft.Json.Linq;

namespace dotnet_new3
{
    internal class NewtonsoftJsonDocumentObjectModel : IJsonDocumentObjectModelFactory
    {
        public bool TryParse(string jsonText, out IJsonToken root)
        {
            try
            {
                JToken token = JToken.Parse(jsonText);
                root = AdaptToken(token, this);
                return true;
            }
            catch
            {
                root = null;
                return false;
            }
        }

        private static IJsonToken AdaptToken(JToken token, IJsonDocumentObjectModelFactory factory)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    return new JObjectAdapter(token, factory);
                case JTokenType.Array:
                    return new JArrayAdapter(token, factory);
                default:
                    return new JValueAdapter(token, factory);
            }
        }

        public IJsonObject CreateObject()
        {
            return new JObjectAdapter(new JObject(), this);
        }

        public IJsonArray CreateArray()
        {
            return new JArrayAdapter(new JArray(), this);
        }

        public IJsonValue CreateValue(int value)
        {
            return new JValueAdapter(JToken.FromObject(value), this);
        }

        public IJsonValue CreateValue(double value)
        {
            return new JValueAdapter(JToken.FromObject(value), this);
        }

        public IJsonValue CreateValue(string value)
        {
            return new JValueAdapter(JToken.FromObject(value), this);
        }

        public IJsonValue CreateValue(bool value)
        {
            return new JValueAdapter(JToken.FromObject(value), this);
        }

        public IJsonValue CreateNull()
        {
            return new JValueAdapter(JValue.CreateNull(), this);
        }

        private class JObjectAdapter : JTokenAdapter, IJsonObject
        {
            private readonly JObject _object;

            public JObjectAdapter(JToken token, IJsonDocumentObjectModelFactory factory)
                : base (token, factory)
            {
                _object = (JObject)token;
            }

            public IEnumerable<string> PropertyNames => _object.Properties().Select(x => x.Name);

            public IReadOnlyCollection<string> ExtractValues(params (string propertyName, Action<IJsonToken> valueExtractor)[] mappings)
            {
                HashSet<string> foundNames = new HashSet<string>(StringComparer.Ordinal);

                foreach ((string name, Action<IJsonToken> valueExtractor) in mappings)
                {
                    if (_object.TryGetValue(name, StringComparison.Ordinal, out JToken token))
                    {
                        foundNames.Add(name);
                        valueExtractor(AdaptToken(token, Factory));
                    }
                }

                return foundNames;
            }

            public IJsonObject RemoveValue(string propertyName)
            {
                _object.Remove(propertyName);
                return this;
            }

            public IJsonObject SetValue(string propertyName, IJsonToken value)
            {
                if (!(value is JTokenAdapter adapter))
                {
                    throw new ArgumentException("Inconsistent json value");
                }

                _object[propertyName] = adapter.Token;
                return this;
            }
        }

        private class JArrayAdapter : JTokenAdapter, IJsonArray
        {
            private readonly JArray _array;

            public JArrayAdapter(JToken token, IJsonDocumentObjectModelFactory factory)
                : base (token, factory)
            {
                _array = (JArray)token;
            }

            public IJsonToken this[int index] => AdaptToken(_array[index], Factory);

            public int Count => _array.Count;

            public IJsonArray Add(IJsonToken value)
            {
                if (!(value is JTokenAdapter adapter))
                {
                    throw new ArgumentException("Inconsistent json value");
                }

                _array.Add(adapter.Token);
                return this;
            }

            public IEnumerator<IJsonToken> GetEnumerator() => _array.Select(x => AdaptToken(x, Factory)).GetEnumerator();

            public IJsonArray RemoveAt(int index)
            {
                _array.RemoveAt(index);
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class JValueAdapter : JTokenAdapter, IJsonValue
        {
            public JValueAdapter(JToken token, IJsonDocumentObjectModelFactory factory)
                : base (token, factory)
            {
            }

            public object Value => Token.Value<object>();
        }

        private class JTokenAdapter : IJsonToken
        {
            public JTokenAdapter(JToken token, IJsonDocumentObjectModelFactory factory)
            {
                Token = token;
                Factory = factory;
            }

            public IJsonDocumentObjectModelFactory Factory { get; }

            public JToken Token { get; }

            public JsonTokenType TokenType => AdaptType(Token.Type);

            private JsonTokenType AdaptType(JTokenType type)
            {
                switch (type)
                {
                    case JTokenType.Array:
                        return JsonTokenType.Array;
                    case JTokenType.Boolean:
                        return JsonTokenType.Boolean;
                    case JTokenType.Float:
                        return JsonTokenType.Number;
                    case JTokenType.Integer:
                        return JsonTokenType.Number;
                    case JTokenType.None:
                        return JsonTokenType.None;
                    case JTokenType.Null:
                        return JsonTokenType.Null;
                    case JTokenType.Object:
                        return JsonTokenType.Object;
                    case JTokenType.String:
                        return JsonTokenType.String;
                    case JTokenType.Undefined:
                        return JsonTokenType.Undefined;
                    default:
                        throw new NotSupportedException($"Unkown token type {type}");
                }
            }

            public void WriteToStream(Stream s)
            {
                string text = Token.ToString();
                byte[] data = Encoding.UTF8.GetBytes(text);
                s.Write(data, 0, data.Length);
            }
        }
    }
}
//#endif
