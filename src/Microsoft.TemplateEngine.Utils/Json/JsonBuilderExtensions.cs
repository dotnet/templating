using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Utils.Json
{
    internal static class JsonBuilderExtensions
    {
        public static DirectDeserialize<TCollection> Deserialize<TCollection, TElement>(Func<TCollection> collectionCreator, Func<TElement> elementCreator, Deserialize<TElement> elementDeserializer, Action<TCollection, TElement> setter)
            => Deserialize(collectionCreator, t => elementDeserializer(t, elementCreator), setter);

        public static DirectDeserialize<TCollection> Deserialize<TCollection, TElement>(Func<TCollection> collectionCreator, DirectDeserialize<TElement> elementDeserializer, Action<TCollection, TElement> setter)
        {
            return t =>
            {
                TCollection result = collectionCreator();

                if (t.TokenType == JsonTokenType.Array)
                {
                    IJsonArray source = (IJsonArray)t;

                    foreach (IJsonToken entry in source)
                    {
                        TElement element = elementDeserializer(entry);
                        setter(result, element);
                    }
                }
                else
                {
                    TElement element = elementDeserializer(t);
                    setter(result, element);
                }

                return result;
            };
        }

        public static DirectDeserialize<TDictionary> Deserialize<TDictionary, TElement>(Func<TDictionary> dictionaryCreator, Func<TElement> elementCreator, Deserialize<TElement> elementDeserializer, Action<TDictionary, string, TElement> setter)
            => Deserialize(dictionaryCreator, t => elementDeserializer(t, elementCreator), setter);

        public static DirectDeserialize<TDictionary> Deserialize<TDictionary, TElement>(Func<TDictionary> dictionaryCreator, DirectDeserialize<TElement> elementDeserializer, Action<TDictionary, string, TElement> setter)
        {
            return t =>
            {
                TDictionary result = dictionaryCreator();
                IJsonObject source = (IJsonObject)t;

                foreach (KeyValuePair<string, IJsonToken> entry in source.Properties())
                {
                    TElement element = elementDeserializer(entry.Value);
                    setter(result, entry.Key, element);
                }

                return result;
            };
        }

        public static bool DeserializeBool(IJsonToken token)
            => (bool)((IJsonValue)token).Value;

        public static Guid DeserializeGuid(IJsonToken token)
            => Guid.Parse(DeserializeString(token));

        public static int DeserializeInt(IJsonToken token)
            => (int)(double)((IJsonValue)token).Value;

        public static DateTime? DeserializeNullableDateTime(IJsonToken token)
        {
            string s = DeserializeString(token);

            if (s is null)
            {
                return null;
            }

            return DateTime.Parse(s, null, DateTimeStyles.RoundtripKind);
        }

        public static string DeserializeString(IJsonToken token)
            => ((IJsonValue)token).Value?.ToString();

        public static DictionaryOperations<T, TConcrete, TElement, TElementConcrete> Dictionary<T, TConcrete, TElement, TElementConcrete>(this JsonBuilder<T, TConcrete> builder, IWrapper<TElement, TElementConcrete> wrapper)
            where TConcrete : T
            where TElementConcrete : TElement
            => new DictionaryOperations<T, TConcrete, TElement, TElementConcrete>(builder, wrapper.Serialize, wrapper.Deserialize);

        public static DictionaryOperations<T, TConcrete, TElement, TElementConcrete> Dictionary<T, TConcrete, TElement, TElementConcrete>(this JsonBuilder<T, TConcrete> builder, Serialize<TElement> serializer, DirectDeserialize<TElementConcrete> deserializer)
            where TConcrete : T
            where TElementConcrete : TElement
            => new DictionaryOperations<T, TConcrete, TElement, TElementConcrete>(builder, serializer, deserializer);

        public static DictionaryOperations<T, TConcrete, TElement, TElementConcrete> Dictionary<T, TConcrete, TElement, TElementConcrete>(this JsonBuilder<T, TConcrete> builder)
            where TConcrete : T
            where TElement : IJsonSerializable<TElement>
            where TElementConcrete : TElement, new()
            => new DictionaryOperations<T, TConcrete, TElement, TElementConcrete>(builder, JsonSerialize<TElement, TElementConcrete>.Serialize, JsonSerialize<TElement, TElementConcrete>.Deserialize);

        public static DictionaryOperations<T, TConcrete, TElement, TElement> Dictionary<T, TConcrete, TElement>(this JsonBuilder<T, TConcrete> builder, Serialize<TElement> serializer, DirectDeserialize<TElement> deserializer)
            where TConcrete : T
            => builder.Dictionary<T, TConcrete, TElement, TElement>(serializer, deserializer);

        public static DictionaryOperations<T, TConcrete, Guid, Guid> DictionaryOfGUid<T, TConcrete>(this JsonBuilder<T, TConcrete> builder)
            where TConcrete : T
            => builder.Dictionary<T, TConcrete, Guid, Guid>(Serialize, DeserializeGuid);

        public static DictionaryOperations<T, TConcrete, int, int> DictionaryOfInt<T, TConcrete>(this JsonBuilder<T, TConcrete> builder)
            where TConcrete : T
            => builder.Dictionary<T, TConcrete, int, int>(Serialize, DeserializeInt);

        public static DictionaryOperations<T, TConcrete, string, string> DictionaryOfString<T, TConcrete>(this JsonBuilder<T, TConcrete> builder)
            where TConcrete : T
            => builder.Dictionary<T, TConcrete, string, string>(Serialize, DeserializeString);

        public static ListOperations<T, TConcrete, TElement, TElementConcrete> List<T, TConcrete, TElement, TElementConcrete>(this JsonBuilder<T, TConcrete> builder, IWrapper<TElement, TElementConcrete> wrapper)
            where TConcrete : T
            where TElementConcrete : TElement
            => new ListOperations<T, TConcrete, TElement, TElementConcrete>(builder, wrapper.Serialize, wrapper.Deserialize);

        public static ListOperations<T, TConcrete, TElement, TElementConcrete> List<T, TConcrete, TElement, TElementConcrete>(this JsonBuilder<T, TConcrete> builder, Serialize<TElement> serializer, DirectDeserialize<TElementConcrete> deserializer)
            where TConcrete : T
            where TElementConcrete : TElement
            => new ListOperations<T, TConcrete, TElement, TElementConcrete>(builder, serializer, deserializer);

        public static ListOperations<T, TConcrete, TElement, TElement> List<T, TConcrete, TElement>(this JsonBuilder<T, TConcrete> builder)
            where TConcrete : T
            where TElement : IJsonSerializable<TElement>, new()
            => builder.List<T, TConcrete, TElement, TElement>(JsonSerialize<TElement, TElement>.Serialize, JsonSerialize<TElement, TElement>.Deserialize);

        public static ListOperations<T, TConcrete, TElement, TElement> List<T, TConcrete, TElement>(this JsonBuilder<T, TConcrete> builder, Serialize<TElement> serializer, DirectDeserialize<TElement> deserializer)
            where TConcrete : T
            => builder.List<T, TConcrete, TElement, TElement>(serializer, deserializer);

        public static ListOperations<T, TConcrete, Guid, Guid> ListOfGuid<T, TConcrete>(this JsonBuilder<T, TConcrete> builder)
            where TConcrete : T
            => builder.List<T, TConcrete, Guid, Guid>(Serialize, DeserializeGuid);

        public static ListOperations<T, TConcrete, int, int> ListOfInt<T, TConcrete>(this JsonBuilder<T, TConcrete> builder)
            where TConcrete : T
            => builder.List<T, TConcrete, int, int>(Serialize, DeserializeInt);

        public static ListOperations<T, TConcrete, string, string> ListOfString<T, TConcrete>(this JsonBuilder<T, TConcrete> builder)
            where TConcrete : T
            => builder.List<T, TConcrete, string, string>(Serialize, DeserializeString);

        public static JsonBuilder<T, TConcrete> Map<T, TConcrete, TValue>(this JsonBuilder<T, TConcrete> builder, Expression<Getter<TConcrete, TValue>> property, string propertyName = null)
            where TConcrete : T
            where TValue : IJsonSerializable<TValue>, new()
            => builder.Map(property, () => new TValue(), propertyName);

        public static JsonBuilder<T, TConcrete> Map<T, TConcrete>(this JsonBuilder<T, TConcrete> builder, Expression<Getter<TConcrete, bool>> parameter, string propertyName = null)
            where TConcrete : T
            => builder.Map(parameter, Serialize, DeserializeBool, propertyName);

        public static JsonBuilder<T, TConcrete> Map<T, TConcrete>(this JsonBuilder<T, TConcrete> builder, Expression<Getter<TConcrete, DateTime?>> parameter, string propertyName = null)
            where TConcrete : T
            => builder.Map(parameter, Serialize, DeserializeNullableDateTime, propertyName);

        public static JsonBuilder<T, TConcrete> Map<T, TConcrete>(this JsonBuilder<T, TConcrete> builder, Expression<Getter<TConcrete, string>> parameter, string propertyName = null)
            where TConcrete : T
            => builder.Map(parameter, Serialize, DeserializeString, propertyName);

        public static JsonBuilder<T, TConcrete> Map<T, TConcrete>(this JsonBuilder<T, TConcrete> builder, Expression<Getter<TConcrete, int>> parameter, string propertyName = null)
            where TConcrete : T
            => builder.Map(parameter, Serialize, DeserializeInt, propertyName);

        public static JsonBuilder<T, TConcrete> Map<T, TConcrete>(this JsonBuilder<T, TConcrete> builder, Expression<Getter<TConcrete, Guid>> parameter, string propertyName = null)
            where TConcrete : T
            => builder.Map(parameter, Serialize, DeserializeGuid, propertyName);

        public static JsonBuilder<T, TConcrete> Map<T, TConcrete, TValue, TValueConcrete>(this JsonBuilder<T, TConcrete> builder, Expression<Getter<TConcrete, TValue>> property, Serialize<TValue> serialize, Deserialize<TValueConcrete> deserialize, Func<TValueConcrete> itemCreator, string propertyName = null)
            where TConcrete : T
            where TValueConcrete : TValue
        {
            ProcessExpression(property, out Getter<TConcrete, TValue> getter, out Setter<TConcrete, TValue> setter, ref propertyName);
            return builder.MapCore(getter, serialize, deserialize, setter, itemCreator, propertyName);
        }

        public static JsonBuilder<T, TConcrete> Map<T, TConcrete, TValue, TValueConcrete>(this JsonBuilder<T, TConcrete> builder, Expression<Getter<TConcrete, TValue>> property, Serialize<TValue> serialize, Deserialize<TValueConcrete> deserialize, string propertyName = null)
            where TConcrete : T
            where TValueConcrete : TValue, new()
            => builder.Map(property, serialize, deserialize, () => new TValueConcrete(), propertyName);

        public static JsonBuilder<T, TConcrete> Map<T, TConcrete, TValue>(this JsonBuilder<T, TConcrete> builder, Expression<Getter<TConcrete, TValue>> property, Serialize<TValue> serializer, DirectDeserialize<TValue> deserializer, string propertyName = null)
            where TConcrete : T
        {
            ProcessExpression(property, out Getter<TConcrete, TValue> getter, out Setter<TConcrete, TValue> setter, ref propertyName);
            return builder.MapCore(getter, serializer, deserializer, setter, propertyName);
        }

        public static JsonBuilder<T, TConcrete> Map<T, TConcrete, TValue>(this JsonBuilder<T, TConcrete> builder, Expression<Getter<TConcrete, TValue>> property, Func<TValue> itemCreator, string propertyName = null)
            where TConcrete : T
            where TValue : IJsonSerializable<TValue>
        {
            JsonSerialize<TValue>.Configure(itemCreator);
            ProcessExpression(property, out Getter<TConcrete, TValue> getter, out Setter<TConcrete, TValue> setter, ref propertyName);
            return builder.MapCore(getter, JsonSerialize<TValue>.Serialize, JsonSerialize<TValue>.Deserialize, setter, propertyName);
        }

        public static IJsonToken NullSerializer<TValue>(IJsonDocumentObjectModelFactory domFactory, TValue item) => null;

        public static void ProcessExpression<T, TValue>(Expression<Getter<T, TValue>> property, out Getter<T, TValue> getter, out Setter<T, TValue> setter, ref string propertyName)
        {
            MemberExpression memberExpression = (MemberExpression)property.Body;
            ParameterExpression targetExpression = (ParameterExpression)memberExpression.Expression;
            getter = Expression.Lambda<Getter<T, TValue>>(memberExpression, targetExpression).Compile();

            ParameterExpression valueExpression = Expression.Parameter(typeof(TValue), "value");
            setter = Expression.Lambda<Setter<T, TValue>>(Expression.Assign(memberExpression, valueExpression), targetExpression, valueExpression).Compile();

            propertyName = propertyName ?? memberExpression.Member.Name;
        }

        public static IJsonToken Serialize(IJsonDocumentObjectModelFactory domFactory, bool item)
            => domFactory.CreateValue(item);

        public static IJsonToken Serialize(IJsonDocumentObjectModelFactory domFactory, int item)
            => domFactory.CreateValue(item);

        public static IJsonToken Serialize(IJsonDocumentObjectModelFactory domFactory, Guid item)
            => domFactory.CreateValue(item.ToString());

        public static IJsonToken Serialize(IJsonDocumentObjectModelFactory domFactory, DateTime? item)
            => item.HasValue ? domFactory.CreateValue(item.Value.ToString("o")) : domFactory.CreateNull();

        public static IJsonToken Serialize(IJsonDocumentObjectModelFactory domFactory, string item)
            => domFactory.CreateValue(item);

        public static Serialize<TCollection> SerializeCollection<TCollection, TElement>(Serialize<TElement> elementSerializer)
                                                            where TCollection : IEnumerable<TElement>
            => SerializeCollection<TCollection, TElement>(x => x, elementSerializer);

        public static Serialize<TCollection> SerializeCollection<TCollection, TElement>(Func<TCollection, IEnumerable<TElement>> getEnumerator, Serialize<TElement> elementSerializer)
        {
            return (domFactory, source) =>
            {
                IJsonArray result = domFactory.CreateArray();

                foreach (TElement entry in getEnumerator(source))
                {
                    result.Add(elementSerializer(domFactory, entry));
                }

                return result;
            };
        }

        public static Serialize<TDictionary> SerializeDictionary<TDictionary, TElement>(Serialize<TElement> elementSerializer)
            where TDictionary : IReadOnlyDictionary<string, TElement>
            => SerializeDictionary<TDictionary, TElement>(x => x, elementSerializer);

        public static Serialize<TDictionary> SerializeDictionary<TDictionary, TElement>(Func<TDictionary, IEnumerable<KeyValuePair<string, TElement>>> getEnumerator, Serialize<TElement> elementSerializer)
        {
            return (domFactory, source) =>
            {
                IJsonObject result = domFactory.CreateObject();

                foreach (KeyValuePair<string, TElement> entry in getEnumerator(source))
                {
                    result.SetValue(entry.Key, elementSerializer(domFactory, entry.Value));
                }

                return result;
            };
        }
    }
}
