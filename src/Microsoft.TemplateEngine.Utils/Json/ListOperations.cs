using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Utils.Json
{
    internal class ListOperations<TResult, TResultConcrete, TElement, TElementConcrete>
        where TResultConcrete : TResult
        where TElementConcrete : TElement
    {
        private readonly JsonBuilder<TResult, TResultConcrete> _builder;
        private readonly DirectDeserialize<TElementConcrete> _elementDeserializer;
        private readonly Serialize<TElement> _elementSerializer;

        public ListOperations(JsonBuilder<TResult, TResultConcrete> builder, Serialize<TElement> serializer, DirectDeserialize<TElementConcrete> deserializer)
        {
            _builder = builder;
            _elementSerializer = serializer;
            _elementDeserializer = deserializer;
        }

        public JsonBuilder<TResult, TResultConcrete> Map<TCollection, TCollectionConcrete>
            (Getter<TResultConcrete, TCollection> getter,
                Setter<TResultConcrete, TCollection> setter,
                Func<TCollectionConcrete> collectionCreator,
                Func<TCollection, IEnumerable<TElement>> getEnumerator,
                Action<TCollectionConcrete, TElementConcrete> addElement,
                string propertyName)
            where TCollectionConcrete : TCollection
            where TCollection : class
        {
            return _builder.MapCore(getter, JsonBuilderExtensions.SerializeCollection(getEnumerator, _elementSerializer), JsonBuilderExtensions.Deserialize(collectionCreator, _elementDeserializer, addElement), setter, propertyName);
        }

        public JsonBuilder<TResult, TResultConcrete> Map<TCollection, TCollectionConcrete>
            (Getter<TResultConcrete, TCollection> getter,
                Setter<TResultConcrete, TCollection> setter,
                Func<TCollectionConcrete> collectionCreator,
                string propertyName)
            where TCollection : class, IEnumerable<TElement>
            where TCollectionConcrete : TCollection, ICollection<TElement>
            => Map(getter, setter, collectionCreator, x => x, (c, v) => c.Add(v), propertyName);

        public JsonBuilder<TResult, TResultConcrete> Map<TCollection, TCollectionConcrete>
            (Getter<TResultConcrete, TCollection> getter,
                Setter<TResultConcrete, TCollection> setter,
                string propertyName)
            where TCollection : class, IEnumerable<TElement>
            where TCollectionConcrete : TCollection, ICollection<TElement>, new()
            => Map(getter, setter, () => new TCollectionConcrete(), x => x, (c, v) => c.Add(v), propertyName);

        public JsonBuilder<TResult, TResultConcrete> Map<TCollection>
            (Getter<TResultConcrete, TCollection> getter,
                Setter<TResultConcrete, TCollection> setter,
                string propertyName)
            where TCollection : class, ICollection<TElement>, new()
            => Map(getter, setter, () => new TCollection(), x => x, (c, v) => c.Add(v), propertyName);
    }
}
