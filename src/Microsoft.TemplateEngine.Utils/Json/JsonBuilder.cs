using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Utils.Json
{
    internal class JsonBuilder<TResult, TResultConcrete> : IJsonBuilder<TResult>
        where TResultConcrete : TResult
    {
        private readonly Func<TResult> _creator;
        private readonly Dictionary<string, Action<IJsonToken, TResult>> _deserializeSteps = new Dictionary<string, Action<IJsonToken, TResult>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Action<TResult, IJsonObject>> _serializeSteps = new List<Action<TResult, IJsonObject>>();

        public JsonBuilder(Func<TResult> creator)
        {
            _creator = creator;
        }

        public TResult Deserialize(IJsonToken source)
        {
            if (source.TokenType == JsonTokenType.Object)
            {
                TResult result = _creator();
                ((IJsonObject)source).ExtractValues(result, _deserializeSteps);
                return result;
            }

            return default(TResult);
        }

        public JsonBuilder<TResult, TResultConcrete> MapCore<T, TConcrete>(Getter<TResultConcrete, T> getter, Serialize<T> serialize, Deserialize<TConcrete> deserialize, Setter<TResultConcrete, T> setter, Func<TConcrete> itemCreator, string propertyName)
            where TConcrete : T
            => MapInternal(getter, serialize, t => deserialize(t, itemCreator), setter, propertyName);

        public JsonBuilder<TResult, TResultConcrete> MapCore<T, TConcrete>(Getter<TResultConcrete, T> getter, Serialize<T> serialize, DirectDeserialize<TConcrete> deserialize, Setter<TResultConcrete, T> setter, string propertyName)
            where TConcrete : T
            => MapInternal(getter, serialize, deserialize, setter, propertyName);

        public IJsonObject Serialize(IJsonDocumentObjectModelFactory domFactory, TResult item)
        {
            IJsonObject obj = domFactory.CreateObject();

            foreach (Action<TResult, IJsonObject> action in _serializeSteps)
            {
                action(item, obj);
            }

            return obj;
        }

        private JsonBuilder<TResult, TResultConcrete> MapInternal<T, TConcrete>(Getter<TResultConcrete, T> getter, Serialize<T> serialize, DirectDeserialize<TConcrete> deserialize, Setter<TResultConcrete, T> setter, string propertyName)
                    where TConcrete : T
        {
            _serializeSteps.Add((source, target) =>
            {
                T value = getter((TResultConcrete)source);
                IJsonToken token = serialize(target.Factory, value);
                target.SetValue(propertyName, token);
            });

            _deserializeSteps[propertyName] = (source, target) => setter((TResultConcrete)target, deserialize(source));

            return this;
        }
    }
}
