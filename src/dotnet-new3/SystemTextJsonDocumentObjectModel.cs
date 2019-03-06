using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.TemplateEngine.Abstractions.Json;
using JsonTokenType = Microsoft.TemplateEngine.Abstractions.Json.JsonTokenType;

namespace dotnet_new3
{
    internal class SystemTextJsonDocumentObjectModel : IJsonDocumentObjectModelFactory
    {
        public bool TryParse(string jsonText, out IJsonToken root)
        {
            try
            {
                JsonDocument doc = JsonDocument.Parse(jsonText);
                JsonElement rootElement = doc.RootElement;
                root = AdaptElement(rootElement);
                return true;
            }
            catch
            {
                root = null;
                return false;
            }
        }

        private static IJsonToken AdaptElement(JsonElement element)
        {
            switch (element.Type)
            {
                case JsonValueType.Array:
                    return new JsonArray(element);
                case JsonValueType.Object:
                    return new JsonObject(element);
                default:
                    return new JsonValueAdapter(element);
            }
        }

        public static JsonTokenType AdaptType(JsonValueType type)
        {
            switch (type)
            {
                case JsonValueType.Array:
                    return JsonTokenType.Array;
                case JsonValueType.False:
                case JsonValueType.True:
                    return JsonTokenType.Boolean;
                case JsonValueType.Null:
                    return JsonTokenType.Null;
                case JsonValueType.Number:
                    return JsonTokenType.Number;
                case JsonValueType.Object:
                    return JsonTokenType.Object;
                case JsonValueType.String:
                    return JsonTokenType.String;
                case JsonValueType.Undefined:
                    return JsonTokenType.Undefined;
                default:
                    throw new NotSupportedException($"Unkown token type {type}");

            }
        }

        public IJsonObject CreateObject() => new JsonObject();

        public IJsonArray CreateArray() => new JsonArray();

        public IJsonValue CreateValue(int value) => new WritableJsonValue(value, JsonTokenType.Number);

        public IJsonValue CreateValue(double value) => new WritableJsonValue(value, JsonTokenType.Number);

        public IJsonValue CreateValue(string value) => new WritableJsonValue(value, JsonTokenType.String);

        public IJsonValue CreateValue(bool value) => new WritableJsonValue(value, JsonTokenType.Boolean);

        public IJsonValue CreateNull() => new WritableJsonValue(null, JsonTokenType.Null);

        private class JsonArray : IJsonArray, IWriteToJsonWriter
        {
            private IJsonArray _basis;

            public JsonArray()
                : this(new WritableJsonArray())
            {
            }

            public JsonArray(JsonElement basis)
                : this(new JsonArrayAdapter(basis))
            {
            }

            private JsonArray(IJsonArray basis)
            {
                _basis = basis;
            }

            public IJsonToken this[int index] => _basis[index];

            public int Count => _basis.Count;

            public JsonTokenType TokenType => JsonTokenType.Array;

            public IJsonArray Add(IJsonToken value)
            {
                _basis = _basis.Add(value);
                return this;
            }

            public IEnumerator<IJsonToken> GetEnumerator() => _basis.GetEnumerator();

            public IJsonArray RemoveAt(int index)
            {
                _basis = _basis.RemoveAt(index);
                return this;
            }

            public void Write(ref Utf8JsonWriter writer) => ((IWriteToJsonWriter)_basis).Write(ref writer);

            public void Write(string propertyName, ref Utf8JsonWriter writer) => ((IWriteToJsonWriter)_basis).Write(propertyName, ref writer);

            public void WriteToStream(Stream s) => _basis.WriteToStream(s);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class JsonObject : IJsonObject, IWriteToJsonWriter
        {
            private IJsonObject _basis;

            public JsonObject()
                : this(new WritableJsonObject())
            {
            }

            public JsonObject(JsonElement basis)
                : this(new JsonObjectAdapter(basis))
            {
            }

            private JsonObject(IJsonObject basis)
            {
                _basis = basis;
            }

            public JsonTokenType TokenType => _basis.TokenType;

            public IEnumerable<string> PropertyNames => _basis.PropertyNames;

            public IReadOnlyCollection<string> ExtractValues(params (string propertyName, Action<IJsonToken> valueExtractor)[] mappings) => _basis.ExtractValues(mappings);

            public IJsonObject RemoveValue(string propertyName)
            {
                _basis = _basis.RemoveValue(propertyName);
                return this;
            }

            public IJsonObject SetValue(string propertyName, IJsonToken value)
            {
                _basis = _basis.SetValue(propertyName, value);
                return this;
            }

            public void Write(ref Utf8JsonWriter writer) => ((IWriteToJsonWriter)_basis).Write(ref writer);

            public void Write(string propertyName, ref Utf8JsonWriter writer) => ((IWriteToJsonWriter)_basis).Write(propertyName, ref writer);

            public void WriteToStream(Stream s) => _basis.WriteToStream(s);
        }

        private abstract class WritableJsonToken : IJsonToken, IWriteToJsonWriter
        {
            public WritableJsonToken(JsonTokenType type)
            {
                TokenType = type;
            }

            public JsonTokenType TokenType { get; }

            public void WriteToStream(Stream s)
            {
                using (StreamBufferWriterImpl bufferWriter = new StreamBufferWriterImpl(s, 1024))
                {
                    Utf8JsonWriter jsonWriter = new Utf8JsonWriter(bufferWriter, new JsonWriterState(new JsonWriterOptions
                    {
                        Indented = false,
                        SkipValidation = true
                    }));
                    Write(ref jsonWriter);
                    jsonWriter.Flush(true);
                }
            }

            public abstract void Write(ref Utf8JsonWriter writer);

            public abstract void Write(string propertyName, ref Utf8JsonWriter writer);
        }

        private class WritableJsonValue : WritableJsonToken, IJsonValue
        {
            public WritableJsonValue(object value, JsonTokenType type)
                : base(type)
            {
                Value = value;
            }

            public object Value { get; }

            public override void Write(ref Utf8JsonWriter writer)
            {
                switch (TokenType)
                {
                    case JsonTokenType.Boolean:
                        writer.WriteBooleanValue((bool)Value);
                        break;
                    case JsonTokenType.Null:
                        writer.WriteNullValue();
                        break;
                    case JsonTokenType.Number:
                        object v = Value;
                        if (v is int integer)
                        {
                            writer.WriteNumberValue(integer);
                        }
                        else
                        {
                            writer.WriteNumberValue((double)v);
                        }
                        break;
                    case JsonTokenType.String:
                        writer.WriteStringValue((string)Value);
                        break;
                    default:
                        throw new NotSupportedException($"Can't write {TokenType} as a value");
                }
            }

            public override void Write(string propertyName, ref Utf8JsonWriter writer)
            {
                switch (TokenType)
                {
                    case JsonTokenType.Boolean:
                        writer.WriteBoolean(propertyName, (bool)Value);
                        break;
                    case JsonTokenType.Null:
                        writer.WriteNull(propertyName);
                        break;
                    case JsonTokenType.Number:
                        object v = Value;
                        if (v is int integer)
                        {
                            writer.WriteNumber(propertyName, integer);
                        }
                        else
                        {
                            writer.WriteNumber(propertyName, (double)v);
                        }
                        break;
                    case JsonTokenType.String:
                        writer.WriteString(propertyName, (string)Value);
                        break;
                    default:
                        throw new NotSupportedException($"Can't write {TokenType} as a value");
                }
            }
        }

        private interface IWriteToJsonWriter
        {
            void Write(ref Utf8JsonWriter writer);

            void Write(string propertyName, ref Utf8JsonWriter writer);
        }

        private class WritableJsonArray : WritableJsonToken, IJsonArray
        {
            private readonly List<IJsonToken> _children;

            public WritableJsonArray()
                : base(JsonTokenType.Array)
            {
                _children = new List<IJsonToken>();
            }

            public WritableJsonArray(JsonElement element)
                : base(JsonTokenType.Array)
            {
                if (element.Type != JsonValueType.Array)
                {
                    throw new ArgumentException("element is not of type array");
                }

                _children = new List<IJsonToken>();

                foreach (JsonElement entry in element.EnumerateArray())
                {
                    _children.Add(AdaptElement(entry));
                }
            }

            public IJsonToken this[int index] => _children[index];

            public int Count => _children.Count;

            public IJsonArray Add(IJsonToken value)
            {
                _children.Add(value);
                return this;
            }

            public IEnumerator<IJsonToken> GetEnumerator() => _children.GetEnumerator();

            public IJsonArray RemoveAt(int index)
            {
                _children.RemoveAt(index);
                return this;
            }

            public override void Write(ref Utf8JsonWriter writer)
            {
                writer.WriteStartArray();
                WriteCore(ref writer);
            }

            public override void Write(string propertyName, ref Utf8JsonWriter writer)
            {
                writer.WriteStartArray(propertyName);
                WriteCore(ref writer);
            }

            private void WriteCore(ref Utf8JsonWriter writer)
            {
                foreach (IJsonToken token in _children)
                {
                    if (!(token is IWriteToJsonWriter writable))
                    {
                        throw new Exception($"Unsupported value source attempting to be written {token}");
                    }

                    writable.Write(ref writer);
                }
                writer.WriteEndArray();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class WritableJsonObject : WritableJsonToken, IJsonObject
        {
            private readonly Dictionary<string, IJsonToken> _properties;

            public WritableJsonObject()
                : base(JsonTokenType.Object)
            {
                _properties = new Dictionary<string, IJsonToken>(StringComparer.Ordinal);
            }

            public WritableJsonObject(JsonElement element)
                : base(JsonTokenType.Object)
            {
                if (element.Type != JsonValueType.Object)
                {
                    throw new ArgumentException("element is not of type object");
                }

                _properties = new Dictionary<string, IJsonToken>(StringComparer.Ordinal);

                foreach (JsonProperty property in element.EnumerateObject())
                {
                    _properties[property.Name] = AdaptElement(property.Value);
                }
            }

            public IEnumerable<string> PropertyNames => _properties.Keys;

            public IReadOnlyCollection<string> ExtractValues(params (string propertyName, Action<IJsonToken> valueExtractor)[] mappings)
            {
                Dictionary<string, Action<IJsonToken>> mapLookup = mappings.ToDictionary(x => x.propertyName, x => x.valueExtractor);
                HashSet<string> foundProperties = new HashSet<string>(StringComparer.Ordinal);

                foreach (KeyValuePair<string, IJsonToken> entry in _properties)
                {
                    if (mapLookup.TryGetValue(entry.Key, out Action<IJsonToken> action))
                    {
                        action(entry.Value);
                        foundProperties.Add(entry.Key);
                    }
                }

                return foundProperties;
            }

            public IJsonObject RemoveValue(string propertyName)
            {
                _properties.Remove(propertyName);
                return this;
            }

            public IJsonObject SetValue(string propertyName, IJsonToken value)
            {
                _properties[propertyName] = value;
                return this;
            }

            public override void Write(ref Utf8JsonWriter writer)
            {
                writer.WriteStartObject();
                WriteCore(ref writer);
            }

            public override void Write(string propertyName, ref Utf8JsonWriter writer)
            {
                writer.WriteStartObject(propertyName);
                WriteCore(ref writer);
            }

            private void WriteCore(ref Utf8JsonWriter writer)
            {
                foreach (KeyValuePair<string, IJsonToken> entry in _properties)
                {
                    if (!(entry.Value is IWriteToJsonWriter writable))
                    {
                        throw new Exception($"Unsupported value source attempting to be written {entry.Key} ({entry.Value})");
                    }

                    writable.Write(entry.Key, ref writer);
                }
                writer.WriteEndObject();
            }
        }

        private abstract class JsonElementAdapter : IJsonToken, IWriteToJsonWriter
        {
            protected readonly JsonElement _element;

            public JsonElementAdapter(JsonElement element)
            {
                _element = element;
                TokenType = AdaptType(_element.Type);
            }

            public JsonTokenType TokenType { get; }

            public abstract void Write(ref Utf8JsonWriter writer);

            public abstract void Write(string propertyName, ref Utf8JsonWriter writer);

            public void WriteToStream(Stream s)
            {
                IBufferWriter<byte> bufferWriter = new StreamBufferWriterImpl(s, 1024);
                Utf8JsonWriter jsonWriter = new Utf8JsonWriter(bufferWriter, new JsonWriterState(new JsonWriterOptions
                {
                    Indented = false,
                    SkipValidation = true
                }));
                Write(ref jsonWriter);

                jsonWriter.Flush(true);
            }
        }

        private class JsonValueAdapter : JsonElementAdapter, IJsonValue
        {
            public JsonValueAdapter(JsonElement element)
                : base(element)
            {
            }

            public object Value
            {
                get
                {
                    switch (_element.Type)
                    {
                        case JsonValueType.False:
                            return false;
                        case JsonValueType.True:
                            return true;
                        case JsonValueType.String:
                            return _element.GetString();
                        case JsonValueType.Number:
                            if (_element.TryGetDouble(out double val))
                            {
                                return val;
                            }
                            return 0d;
                        default:
                            return null;
                    }
                }
            }

            public override void Write(ref Utf8JsonWriter writer)
            {
                switch (TokenType)
                {
                    case JsonTokenType.Boolean:
                        writer.WriteBooleanValue((bool)Value);
                        break;
                    case JsonTokenType.Null:
                        writer.WriteNullValue();
                        break;
                    case JsonTokenType.Number:
                        object v = Value;

                        if (v is int integer)
                        {
                            writer.WriteNumberValue(integer);
                        }
                        else
                        {
                            writer.WriteNumberValue((double)v);
                        }
                        break;
                    case JsonTokenType.String:
                        writer.WriteStringValue((string)Value);
                        break;
                    default:
                        throw new NotSupportedException($"Cannot write type {TokenType}");
                }
            }

            public override void Write(string propertyName, ref Utf8JsonWriter writer)
            {
                switch (TokenType)
                {
                    case JsonTokenType.Boolean:
                        writer.WriteBoolean(propertyName, (bool)Value);
                        break;
                    case JsonTokenType.Null:
                        writer.WriteNull(propertyName);
                        break;
                    case JsonTokenType.Number:
                        object v = Value;

                        if (v is int integer)
                        {
                            writer.WriteNumber(propertyName, integer);
                        }
                        else
                        {
                            writer.WriteNumber(propertyName, (double)v);
                        }
                        break;
                    case JsonTokenType.String:
                        writer.WriteString(propertyName, (string)Value);
                        break;
                    default:
                        throw new NotSupportedException($"Cannot write type {TokenType}");
                }
            }
        }

        private class JsonArrayAdapter : JsonElementAdapter, IJsonArray
        {
            public JsonArrayAdapter(JsonElement element)
                : base(element)
            {
            }

            public IJsonToken this[int index] => AdaptElement(_element.EnumerateArray().ElementAt(index));

            public int Count => _element.GetArrayLength();

            public IJsonArray Add(IJsonToken value)
            {
                return new WritableJsonArray(_element)
                    .Add(value);
            }

            public IEnumerator<IJsonToken> GetEnumerator() => _element.EnumerateArray().Select(AdaptElement).GetEnumerator();

            public IJsonArray RemoveAt(int index)
            {
                return new WritableJsonArray(_element)
                    .RemoveAt(index);
            }

            public override void Write(ref Utf8JsonWriter writer)
            {
                writer.WriteStartArray();
                WriteCore(ref writer);
            }

            public override void Write(string propertyName, ref Utf8JsonWriter writer)
            {
                writer.WriteStartArray(propertyName);
                WriteCore(ref writer);
            }

            private void WriteCore(ref Utf8JsonWriter writer)
            {
                foreach (IJsonToken token in this)
                {
                    if (!(token is IWriteToJsonWriter writable))
                    {
                        throw new Exception($"Unsupported value source attempting to be written {token}");
                    }

                    writable.Write(ref writer);
                }
                writer.WriteEndArray();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class JsonObjectAdapter : JsonElementAdapter, IJsonObject
        {
            public JsonObjectAdapter(JsonElement element)
                : base(element)
            {
            }

            public IEnumerable<string> PropertyNames => _element.EnumerateObject().Select(x => x.Name);

            public IReadOnlyCollection<string> ExtractValues(params (string propertyName, Action<IJsonToken> valueExtractor)[] mappings)
            {
                Dictionary<string, Action<IJsonToken>> mapLookup = mappings.ToDictionary(x => x.propertyName, x => x.valueExtractor);
                HashSet<string> foundProperties = new HashSet<string>(StringComparer.Ordinal);

                foreach (JsonProperty property in _element.EnumerateObject())
                {
                    if (mapLookup.TryGetValue(property.Name, out Action<IJsonToken> handler))
                    {
                        foundProperties.Add(property.Name);
                        handler(AdaptElement(property.Value));
                    }

                    if (foundProperties.Count == mappings.Length)
                    {
                        break;
                    }
                }

                return foundProperties;
            }

            public IJsonObject RemoveValue(string propertyName)
            {
                return new WritableJsonObject(_element)
                    .RemoveValue(propertyName);
            }

            public IJsonObject SetValue(string propertyName, IJsonToken value)
            {
                return new WritableJsonObject(_element)
                    .SetValue(propertyName, value);
            }

            public override void Write(ref Utf8JsonWriter writer)
            {
                writer.WriteStartObject();
                WriteCore(ref writer);
            }

            public override void Write(string propertyName, ref Utf8JsonWriter writer)
            {
                writer.WriteStartObject(propertyName);
                WriteCore(ref writer);
            }

            private void WriteCore(ref Utf8JsonWriter writer)
            {
                foreach (JsonProperty entry in _element.EnumerateObject())
                {
                    IJsonToken token = AdaptElement(entry.Value);

                    if (!(token is IWriteToJsonWriter writable))
                    {
                        throw new Exception($"Unsupported value source attempting to be written {token}");
                    }

                    writable.Write(entry.Name, ref writer);
                }
                writer.WriteEndObject();
            }
        }
    }
}
