using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Utils.Json
{
    internal class DictionaryOperations<TResult, TResultConcrete, TElement, TElementConcrete>
        where TResultConcrete : TResult
        where TElementConcrete : TElement
    {
        private readonly JsonBuilder<TResult, TResultConcrete> _builder;
        private readonly DirectDeserialize<TElementConcrete> _elementDeserializer;
        private readonly Serialize<TElement> _elementSerializer;

        public DictionaryOperations(JsonBuilder<TResult, TResultConcrete> builder, Serialize<TElement> serializer, DirectDeserialize<TElementConcrete> deserializer)
        {
            _builder = builder;
            _elementDeserializer = deserializer;
            _elementSerializer = serializer;
        }

        public JsonBuilder<TResult, TResultConcrete> Map<TDictionary, TDictionaryConcrete>
            (Expression<Getter<TResultConcrete, TDictionary>> property,
                Func<TDictionaryConcrete> dictionaryCreator,
                Func<TDictionary, IEnumerable<KeyValuePair<string, TElement>>> getEnumerator,
                Action<TDictionaryConcrete, string, TElementConcrete> setElement,
                string propertyName = null)
            where TDictionaryConcrete : TDictionary
        {
            JsonBuilderExtensions.ProcessExpression(property, out Getter<TResultConcrete, TDictionary> getter, out Setter<TResultConcrete, TDictionary> setter, ref propertyName);
            return _builder.MapCore(getter, JsonBuilderExtensions.SerializeDictionary(getEnumerator, _elementSerializer), JsonBuilderExtensions.Deserialize(dictionaryCreator, _elementDeserializer, setElement), setter, propertyName);
        }

        public JsonBuilder<TResult, TResultConcrete> Map<TDictionary, TDictionaryConcrete>
            (Expression<Getter<TResultConcrete, TDictionary>> property,
                Func<TDictionaryConcrete> collectionCreator,
                string propertyName = null)
            where TDictionary : IEnumerable<KeyValuePair<string, TElement>>
            where TDictionaryConcrete : TDictionary, IDictionary<string, TElement>
            => Map(property, collectionCreator, x => x, (c, k, v) => c[k] = v, propertyName);

        public JsonBuilder<TResult, TResultConcrete> Map<TCollection, TCollectionConcrete>
            (Expression<Getter<TResultConcrete, TCollection>> property,
                string propertyName = null)
            where TCollection : IEnumerable<KeyValuePair<string, TElement>>
            where TCollectionConcrete : TCollection, IDictionary<string, TElement>, new()
            => Map(property, () => new TCollectionConcrete(), x => x, (c, k, v) => c[k] = v, propertyName);
    }
}
