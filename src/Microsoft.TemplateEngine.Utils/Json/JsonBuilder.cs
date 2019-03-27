using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Utils.Json
{
    internal class JsonBuilder<T, TConcrete> : IJsonBuilder<T>
        where TConcrete : T
    {
        private readonly Func<T> _creator;
        private readonly Dictionary<string, Action<IJsonToken, T>> _deserializeSteps = new Dictionary<string, Action<IJsonToken, T>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Action<T, IJsonObject>> _serializeSteps = new List<Action<T, IJsonObject>>();

        public JsonBuilder(Func<T> creator)
        {
            _creator = creator;
        }

        public T Deserialize(IJsonToken source)
        {
            if (source.TokenType == JsonTokenType.Object)
            {
                T result = _creator();
                ((IJsonObject)source).ExtractValues(result, _deserializeSteps);
                return result;
            }

            return default(T);
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

            _deserializeSteps[propertyName] = (source, target) => setter((TConcrete)target, deserialize(source));

            return this;
        }
    }
}
