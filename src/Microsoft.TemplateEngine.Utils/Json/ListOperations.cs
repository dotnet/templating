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
            (Expression<Getter<TResultConcrete, TCollection>> property,
                Func<TCollectionConcrete> collectionCreator,
                Func<TCollection, IEnumerable<TElement>> getEnumerator,
                Action<TCollectionConcrete, TElementConcrete> addElement,
                string propertyName = null)
            where TCollectionConcrete : TCollection
        {
            JsonBuilderExtensions.ProcessExpression(property, out Getter<TResultConcrete, TCollection> getter, out Setter<TResultConcrete, TCollection> setter, ref propertyName);
            return _builder.MapCore(getter, JsonBuilderExtensions.SerializeCollection(getEnumerator, _elementSerializer), JsonBuilderExtensions.Deserialize(collectionCreator, _elementDeserializer, addElement), setter, propertyName);
        }

        public JsonBuilder<TResult, TResultConcrete> Map<TCollection, TCollectionConcrete>
            (Expression<Getter<TResultConcrete, TCollection>> property,
                Func<TCollectionConcrete> collectionCreator,
                string propertyName = null)
            where TCollection : IEnumerable<TElement>
            where TCollectionConcrete : TCollection, ICollection<TElement>
            => Map(property, collectionCreator, x => x, (c, v) => c.Add(v), propertyName);

        public JsonBuilder<TResult, TResultConcrete> Map<TCollection, TCollectionConcrete>
            (Expression<Getter<TResultConcrete, TCollection>> property,
                string propertyName = null)
            where TCollection : IEnumerable<TElement>
            where TCollectionConcrete : TCollection, ICollection<TElement>, new()
            => Map(property, () => new TCollectionConcrete(), x => x, (c, v) => c.Add(v), propertyName);

        public JsonBuilder<TResult, TResultConcrete> Map<TCollection>
            (Expression<Getter<TResultConcrete, TCollection>> property,
                string propertyName = null)
            where TCollection : ICollection<TElement>, new()
            => Map(property, () => new TCollection(), x => x, (c, v) => c.Add(v), propertyName);
    }
}
