// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETCOREAPP3_0
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
                : base(token, factory)
            {
                _object = (JObject)token;
            }

            public IEnumerable<string> PropertyNames => _object.Properties().Select(x => x.Name);

            public new IJsonObject Clone() => new JObjectAdapter(_object.DeepClone(), Factory);

            IJsonToken IJsonToken.Clone() => Clone();

            public ISet<string> ExtractValues(IReadOnlyDictionary<string, Action<IJsonToken>> mappings)
            {
                HashSet<string> foundNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (KeyValuePair<string, Action<IJsonToken>> entry in mappings)
                {
                    if (_object.TryGetValue(entry.Key, StringComparison.OrdinalIgnoreCase, out JToken token))
                    {
                        foundNames.Add(entry.Key);
                        entry.Value(AdaptToken(token, Factory));
                    }
                }

                return foundNames;
            }

            public ISet<string> ExtractValues<T>(T context, IReadOnlyDictionary<string, Action<IJsonToken, T>> mappings)
            {
                HashSet<string> foundNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (KeyValuePair<string, Action<IJsonToken, T>> entry in mappings)
                {
                    if (_object.TryGetValue(entry.Key, StringComparison.OrdinalIgnoreCase, out JToken token))
                    {
                        foundNames.Add(entry.Key);
                        entry.Value(AdaptToken(token, Factory), context);
                    }
                }

                return foundNames;
            }

            public IJsonObject Merge(IJsonObject other)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<KeyValuePair<string, IJsonToken>> Properties()
            {
                foreach (JProperty property in _object.Properties())
                {
                    yield return new KeyValuePair<string, IJsonToken>(property.Name, AdaptToken(property.Value, Factory));
                }
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
                : base(token, factory)
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

            public new IJsonArray Clone() => new JArrayAdapter(_array.DeepClone(), Factory);

            IJsonToken IJsonToken.Clone() => Clone();

            public IEnumerator<IJsonToken> GetEnumerator() => _array.Select(t => AdaptToken(t, Factory)).GetEnumerator();

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
                : base(token, factory)
            {
            }

            public object Value
            {
                get
                {
                    object val = ((JValue)Token).Value;
                    switch (Token.Type)
                    {
                        case JTokenType.Integer:
                            return (double)(long)val;
                        default:
                            return val;
                    }
                }
            }

            public new IJsonValue Clone() => new JValueAdapter(Token.DeepClone(), Factory);

            IJsonToken IJsonToken.Clone() => Clone();
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

            public virtual IJsonToken Clone() => null;

            public void WriteToStream(Stream s)
            {
                string text = Token.ToString();
                byte[] data = Encoding.UTF8.GetBytes(text);
                s.Write(data, 0, data.Length);
            }
        }
    }
}
#endif
