using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Utils.Json
{
    internal class JsonBuilder<T, TConcrete> : IJsonBuilder<T>
        where TConcrete : T
    {
        private readonly Func<TConcrete> _creator;
        private readonly Dictionary<string, Action<IJsonToken, TConcrete>> _deserializeSteps = new Dictionary<string, Action<IJsonToken, TConcrete>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Action<T, IJsonObject>> _serializeSteps = new List<Action<T, IJsonObject>>();

        public JsonBuilder(Func<TConcrete> creator)
        {
            _creator = creator;
        }

        public T Deserialize(IJsonToken source)
        {
            if (source.TokenType == JsonTokenType.Object)
            {
                using (var enumerator = ((IJsonObject)source).Properties().GetEnumerator())
                {
                    return Deserialize(null, enumerator);
                }
            }

            return default;
        }

        public T Deserialize(IEnumerable<KeyValuePair<string, IJsonToken>> stashedTokens, IEnumerator<KeyValuePair<string, IJsonToken>> remainingProperties)
        {
            TConcrete result = _creator();

            if (!(stashedTokens is null))
            {
                foreach (KeyValuePair<string, IJsonToken> stashedToken in stashedTokens)
                {
                    if (_deserializeSteps.TryGetValue(stashedToken.Key, out Action<IJsonToken, TConcrete> handler))
                    {
                        handler(stashedToken.Value, result);
                    }
                }
            }

            while (remainingProperties.MoveNext())
            {
                KeyValuePair<string, IJsonToken> current = remainingProperties.Current;
                if (_deserializeSteps.TryGetValue(current.Key, out Action<IJsonToken, TConcrete> handler))
                {
                    handler(current.Value, result);
                }
            }

            return result;
        }

        public JsonBuilder<T, TConcrete> MapCore<TValue, TValueConcrete>(Getter<TConcrete, TValue> getter, Serialize<TValue> serialize, Deserialize<TValueConcrete> deserialize, Setter<TConcrete, TValue> setter, Func<TValueConcrete> itemCreator, string propertyName)
            where TValueConcrete : TValue
            => MapInternal(getter, serialize, t => deserialize(t, itemCreator), setter, propertyName);

        public JsonBuilder<T, TConcrete> MapCore<TValue, TValueConcrete>(Getter<TConcrete, TValue> getter, Serialize<TValue> serialize, DirectDeserialize<TValueConcrete> deserialize, Setter<TConcrete, TValue> setter, string propertyName)
            where TValueConcrete : TValue
            => MapInternal(getter, serialize, deserialize, setter, propertyName);

        public IJsonObject Serialize(IJsonDocumentObjectModelFactory domFactory, T item)
        {
            IJsonObject obj = domFactory.CreateObject();

            foreach (Action<T, IJsonObject> action in _serializeSteps)
            {
                action(item, obj);
            }

            return obj;
        }

        private JsonBuilder<T, TConcrete> MapInternal<TValue, TValueConcrete>(Getter<TConcrete, TValue> getter, Serialize<TValue> serialize, DirectDeserialize<TValueConcrete> deserialize, Setter<TConcrete, TValue> setter, string propertyName)
            where TValueConcrete : TValue
        {
            _serializeSteps.Add((source, target) =>
            {
                TValue value = getter((TConcrete)source);
                IJsonToken token = serialize(target.Factory, value);
                target.SetValue(propertyName, token);
            });

            _deserializeSteps[propertyName] = (source, target) => setter(target, deserialize(source));

            return this;
        }
    }
}
