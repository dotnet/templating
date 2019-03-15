using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Utils.Json
{
    public class TokenBuilder<T>
    {
        private readonly T _parent;
        private string _arrayContextName;
        private TokenBuilder<TokenBuilder<T>> _arrayBuilder;
        private Action<bool, DeserializationContext> _boolDeserializer;
        private Action<DeserializationContext> _nullDeserializer;
        private Action<double, DeserializationContext> _numberDeserializer;
        private ObjectBuilder<TokenBuilder<T>> _objectBuilder;
        private Action<string, DeserializationContext> _stringDeserializer;

        public TokenBuilder(T parent)
        {
            _parent = parent;
        }

        public T Pop() => _parent;

        public TokenBuilder<TokenBuilder<T>> IfArray(string storeAs)
        {
            if (!(_arrayBuilder is null))
            {
                throw new InvalidOperationException("Duplicate handler specification");
            }

            return _arrayBuilder = new TokenBuilder<TokenBuilder<T>>(this) { _arrayContextName = storeAs };
        }

        public TokenBuilder<T> IfBool(string storeAs, Func<bool, object> handler = null)
        {
            if (!(_boolDeserializer is null))
            {
                throw new InvalidOperationException("Duplicate handler specification");
            }

            handler = handler ?? (b => b);
            _boolDeserializer = (b, c) => c[storeAs] = handler(b);
            return this;
        }

        public TokenBuilder<T> IfBool(Action<bool, DeserializationContext> handler)
        {
            if (!(_boolDeserializer is null))
            {
                throw new InvalidOperationException("Duplicate handler specification");
            }

            _boolDeserializer = handler;
            return this;
        }

        public TokenBuilder<T> IfNull(string storeAs, Func<object> handler = null)
        {
            if (!(_nullDeserializer is null))
            {
                throw new InvalidOperationException("Duplicate handler specification");
            }

            handler = handler ?? (() => null);
            _nullDeserializer = (c) => c[storeAs] = handler();
            return this;
        }

        public TokenBuilder<T> IfNull(Action<DeserializationContext> handler)
        {
            if (!(_nullDeserializer is null))
            {
                throw new InvalidOperationException("Duplicate handler specification");
            }

            _nullDeserializer = handler;
            return this;
        }

        public TokenBuilder<T> IfNumber(string storeAs, Func<double, object> handler = null)
        {
            if (!(_numberDeserializer is null))
            {
                throw new InvalidOperationException("Duplicate handler specification");
            }

            handler = handler ?? (b => b);
            _numberDeserializer = (b, c) => c[storeAs] = handler(b);
            return this;
        }

        public TokenBuilder<T> IfNumber(Action<double, DeserializationContext> handler)
        {
            if (!(_numberDeserializer is null))
            {
                throw new InvalidOperationException("Duplicate handler specification");
            }

            _numberDeserializer = handler;
            return this;
        }

        public TokenBuilder<T> IfString(string storeAs, Func<string, object> handler = null)
        {
            if (!(_stringDeserializer is null))
            {
                throw new InvalidOperationException("Duplicate handler specification");
            }

            handler = handler ?? (b => b);
            _stringDeserializer = (b, c) => c[storeAs] = handler(b);
            return this;
        }

        public TokenBuilder<T> IfString(Action<string, DeserializationContext> handler)
        {
            if (!(_stringDeserializer is null))
            {
                throw new InvalidOperationException("Duplicate handler specification");
            }

            _stringDeserializer = handler;
            return this;
        }

        public ObjectBuilder<TokenBuilder<T>> IfObject()
        {
            if (!(_objectBuilder is null))
            {
                throw new InvalidOperationException("Duplicate handler specification");
            }

            return _objectBuilder = _objectBuilder ?? new ObjectBuilder<TokenBuilder<T>>(this);
        }

        private Action<IJsonArray, DeserializationContext> DefineNestedArrayPlan()
        {
            Action<IJsonToken, DeserializationContext> elementDeserializer = DefineNestedPlan().PopulateContext;

            return (a, c) =>
            {
                List<DeserializationContext> childContexts = new List<DeserializationContext>();
                c[_arrayContextName] = childContexts;

                foreach (IJsonToken element in a)
                {
                    DeserializationContext child = new DeserializationContext();
                    childContexts.Add(child);
                    elementDeserializer(element, child);
                }
            };
        }

        public DeserializationPlan<TResult> ToPlan<TResult>(Func<DeserializationContext, TResult> assembler)
        {
            Action<IJsonObject, DeserializationContext> objectDeserailizer = _objectBuilder?.CreateDeserializer();
            Action<IJsonArray, DeserializationContext> arrayDeserializer = _arrayBuilder?.DefineNestedArrayPlan();
            return new DeserializationPlan<TResult>(_boolDeserializer, _nullDeserializer, _numberDeserializer, _stringDeserializer, objectDeserailizer, arrayDeserializer, assembler);
        }

        internal DeserializationPlan DefineNestedPlan()
        {
            Action<IJsonObject, DeserializationContext> objectDeserailizer = _objectBuilder?.CreateDeserializer();
            Action<IJsonArray, DeserializationContext> arrayDeserializer = _arrayBuilder?.DefineNestedArrayPlan();
            return new DeserializationPlan(_boolDeserializer, _nullDeserializer, _numberDeserializer, _stringDeserializer, objectDeserailizer, arrayDeserializer);
        }
    }

    public class DeserializationPlan<T> : DeserializationPlan
    {
        private readonly Func<DeserializationContext, T> _assembler;

        public DeserializationPlan(
            Action<bool, DeserializationContext> boolDeserializer,
            Action<DeserializationContext> nullDeserializer,
            Action<double, DeserializationContext> numberDeserializer,
            Action<string, DeserializationContext> stringDeserializer,
            Action<IJsonObject, DeserializationContext> objectDeserailizer,
            Action<IJsonArray, DeserializationContext> arrayDeserailizer,
            Func<DeserializationContext, T> assembler)
            : base(boolDeserializer, nullDeserializer, numberDeserializer, stringDeserializer, objectDeserailizer, arrayDeserailizer)
        {
            _assembler = assembler;
        }

        public T Deserialize(IJsonToken token)
        {
            DeserializationContext context = new DeserializationContext();
            PopulateContext(token, context);
            return _assembler(context);
        }
    }

    public class DeserializationPlan
    {
        private readonly Dictionary<JsonTokenType, Action<IJsonToken, DeserializationContext>> _tokenTypeHandlers = new Dictionary<JsonTokenType, Action<IJsonToken, DeserializationContext>>();

        public DeserializationPlan(
            Action<bool, DeserializationContext> boolDeserializer,
            Action<DeserializationContext> nullDeserializer,
            Action<double, DeserializationContext> numberDeserializer,
            Action<string, DeserializationContext> stringDeserializer,
            Action<IJsonObject, DeserializationContext> objectDeserializer,
            Action<IJsonArray, DeserializationContext> arrayDeserializer)
        {
            if (!(boolDeserializer is null))
            {
                _tokenTypeHandlers[JsonTokenType.Boolean] = (t, d) => boolDeserializer((bool)((IJsonValue)t).Value, d);
            }

            if (!(nullDeserializer is null))
            {
                _tokenTypeHandlers[JsonTokenType.Null] = (t, d) => nullDeserializer(d);
            }

            if (!(numberDeserializer is null))
            {
                _tokenTypeHandlers[JsonTokenType.Number] = (t, d) => numberDeserializer((double)((IJsonValue)t).Value, d);
            }

            if (!(stringDeserializer is null))
            {
                _tokenTypeHandlers[JsonTokenType.String] = (t, d) => stringDeserializer((string)((IJsonValue)t).Value, d);
            }

            if (!(objectDeserializer is null))
            {
                _tokenTypeHandlers[JsonTokenType.Object] = (t, d) => objectDeserializer((IJsonObject)t, d);
            }

            if (!(arrayDeserializer is null))
            {
                _tokenTypeHandlers[JsonTokenType.Array] = (t, d) => arrayDeserializer((IJsonArray)t, d);
            }
        }

        public void PopulateContext(IJsonToken token, DeserializationContext context)
        {
            if (!_tokenTypeHandlers.TryGetValue(token.TokenType, out Action<IJsonToken, DeserializationContext> handler))
            {
                throw new NotSupportedException($"No plan has been established for token type {token.TokenType}");
            }

            handler(token, context);
        }
    }

    public class ObjectBuilder<T>
    {
        private static readonly Action<IJsonObject, DeserializationContext> DefaultHandler = (o, c) => { };
        private readonly T _parent;
        private readonly Dictionary<string, TokenBuilder<ObjectBuilder<T>>> _propertyBuilders = new Dictionary<string, TokenBuilder<ObjectBuilder<T>>>(StringComparer.OrdinalIgnoreCase);
        private TokenBuilder<ObjectBuilder<T>> _elementDeserializer;
        private string _storeAs;

        public ObjectBuilder(T parent)
        {
            _parent = parent;
        }

        public TokenBuilder<ObjectBuilder<T>> Property(string propertyName)
        {
            if (_propertyBuilders.ContainsKey(propertyName))
            {
                throw new InvalidOperationException("Property name has already been assigned a deserializer");
            }

            return _propertyBuilders[propertyName] = new TokenBuilder<ObjectBuilder<T>>(this);
        }

        public TokenBuilder<ObjectBuilder<T>> StoreAsDictionary(string storeAs)
        {
            if (!(_elementDeserializer is null))
            {
                throw new InvalidOperationException("AsDictionary has already been specified for this object");
            }

            _storeAs = storeAs;
            return _elementDeserializer = new TokenBuilder<ObjectBuilder<T>>(this);
        }

        internal Action<IJsonObject, DeserializationContext> CreateDeserializer()
        {
            Action<IJsonObject, DeserializationContext> declaredProperties = null;
            Action<IJsonObject, DeserializationContext> asDictionary = null;

            if (_propertyBuilders.Count > 0)
            {
                Dictionary<string, Action<IJsonToken, DeserializationContext>> x = new Dictionary<string, Action<IJsonToken, DeserializationContext>>(StringComparer.OrdinalIgnoreCase);

                foreach (KeyValuePair<string, TokenBuilder<ObjectBuilder<T>>> entry in _propertyBuilders)
                {
                    x[entry.Key] = entry.Value.DefineNestedPlan().PopulateContext;
                }

                declaredProperties = (o, c) => o.ExtractValues(c, x);
            }

            if (!(_elementDeserializer is null))
            {
                Action<IJsonToken, DeserializationContext> contextPopulator = _elementDeserializer.DefineNestedPlan().PopulateContext;
                asDictionary = (o, c) =>
                {
                    Dictionary<string, DeserializationContext> childContext = new Dictionary<string, DeserializationContext>(StringComparer.OrdinalIgnoreCase);
                    c[_storeAs] = childContext;

                    foreach (KeyValuePair<string, IJsonToken> entry in o.Properties())
                    {
                        DeserializationContext child = new DeserializationContext();
                        childContext[entry.Key] = child;
                        contextPopulator(entry.Value, child);
                    }
                };
            }

            return asDictionary is null
                ? declaredProperties ?? DefaultHandler
                : declaredProperties is null
                    ? asDictionary
                    : (o, c) =>
                    {
                        asDictionary(o, c);
                        declaredProperties(o, c);
                    };
        }

        public T Pop() => _parent;
    }

    public static class Deserializer
    {
        public static TokenBuilder<object> CreateDeserializerBuilder() => new TokenBuilder<object>(null);
    }

    public class DeserializationContext
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public object this[string key]
        {
            set => _data[key] = value;
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (!_data.TryGetValue(key, out object val))
            {
                return defaultValue;
            }

            if (!(val is T value))
            {
                return defaultValue;
            }

            return value;
        }

        public IEnumerable<string> Keys => _data.Keys;

        public int Count => _data.Count;

        public IReadOnlyDictionary<string, T> ToDictionary<T>(Func<object, T> transformer = null)
        {
            Dictionary<string, T> result = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
            transformer = transformer ?? (o => (T)o);

            foreach (KeyValuePair<string, object> entry in _data)
            {
                result[entry.Key] = transformer(entry.Value);
            }

            return result;
        }

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, T>> ToNestedDictionary<T>(Func<object, T> transformer = null)
        {
            Dictionary<string, IReadOnlyDictionary<string, T>> result = new Dictionary<string, IReadOnlyDictionary<string, T>>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, object> entry in _data)
            {
                DeserializationContext child = (DeserializationContext)entry.Value;
                result[entry.Key] = child.ToDictionary(transformer);
            }

            return result;
        }
    }
}
