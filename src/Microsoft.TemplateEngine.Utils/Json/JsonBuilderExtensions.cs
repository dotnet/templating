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

        public static DictionaryOperations<TResult, TResultConcrete, TElement, TElementConcrete> Dictionary<TResult, TResultConcrete, TElement, TElementConcrete>(this JsonBuilder<TResult, TResultConcrete> builder, IWrapper<TElement, TElementConcrete> wrapper)
            where TResultConcrete : TResult
            where TElementConcrete : TElement
            => new DictionaryOperations<TResult, TResultConcrete, TElement, TElementConcrete>(builder, wrapper.Serialize, wrapper.Deserialize);

        public static DictionaryOperations<TResult, TResultConcrete, TElement, TElementConcrete> Dictionary<TResult, TResultConcrete, TElement, TElementConcrete>(this JsonBuilder<TResult, TResultConcrete> builder, Serialize<TElement> serializer, DirectDeserialize<TElementConcrete> deserializer)
            where TResultConcrete : TResult
            where TElementConcrete : TElement
            => new DictionaryOperations<TResult, TResultConcrete, TElement, TElementConcrete>(builder, serializer, deserializer);

        public static DictionaryOperations<TResult, TResultConcrete, TElement, TElementConcrete> Dictionary<TResult, TResultConcrete, TElement, TElementConcrete>(this JsonBuilder<TResult, TResultConcrete> builder)
            where TResultConcrete : TResult
            where TElement : IJsonSerializable<TElement>
            where TElementConcrete : TElement, new()
            => new DictionaryOperations<TResult, TResultConcrete, TElement, TElementConcrete>(builder, JsonSerialize<TElement, TElementConcrete>.Serialize, JsonSerialize<TElement, TElementConcrete>.Deserialize);

        public static DictionaryOperations<TResult, TResultConcrete, TElement, TElement> Dictionary<TResult, TResultConcrete, TElement>(this JsonBuilder<TResult, TResultConcrete> builder, Serialize<TElement> serializer, DirectDeserialize<TElement> deserializer)
            where TResultConcrete : TResult
            => builder.Dictionary<TResult, TResultConcrete, TElement, TElement>(serializer, deserializer);

        public static DictionaryOperations<TResult, TResultConcrete, Guid, Guid> DictionaryOfGUid<TResult, TResultConcrete>(this JsonBuilder<TResult, TResultConcrete> builder)
            where TResultConcrete : TResult
            => builder.Dictionary<TResult, TResultConcrete, Guid, Guid>(Serialize, DeserializeGuid);

        public static DictionaryOperations<TResult, TResultConcrete, int, int> DictionaryOfInt<TResult, TResultConcrete>(this JsonBuilder<TResult, TResultConcrete> builder)
            where TResultConcrete : TResult
            => builder.Dictionary<TResult, TResultConcrete, int, int>(Serialize, DeserializeInt);

        public static DictionaryOperations<TResult, TResultConcrete, string, string> DictionaryOfString<TResult, TResultConcrete>(this JsonBuilder<TResult, TResultConcrete> builder)
            where TResultConcrete : TResult
            => builder.Dictionary<TResult, TResultConcrete, string, string>(Serialize, DeserializeString);

        public static ListOperations<TResult, TResultConcrete, TElement, TElementConcrete> List<TResult, TResultConcrete, TElement, TElementConcrete>(this JsonBuilder<TResult, TResultConcrete> builder, IWrapper<TElement, TElementConcrete> wrapper)
            where TResultConcrete : TResult
            where TElementConcrete : TElement
            => new ListOperations<TResult, TResultConcrete, TElement, TElementConcrete>(builder, wrapper.Serialize, wrapper.Deserialize);

        public static ListOperations<TResult, TResultConcrete, TElement, TElementConcrete> List<TResult, TResultConcrete, TElement, TElementConcrete>(this JsonBuilder<TResult, TResultConcrete> builder, Serialize<TElement> serializer, DirectDeserialize<TElementConcrete> deserializer)
            where TResultConcrete : TResult
            where TElementConcrete : TElement
            => new ListOperations<TResult, TResultConcrete, TElement, TElementConcrete>(builder, serializer, deserializer);

        public static ListOperations<TResult, TResultConcrete, TElement, TElement> List<TResult, TResultConcrete, TElement>(this JsonBuilder<TResult, TResultConcrete> builder)
            where TResultConcrete : TResult
            where TElement : IJsonSerializable<TElement>, new()
            => builder.List<TResult, TResultConcrete, TElement, TElement>(JsonSerialize<TElement, TElement>.Serialize, JsonSerialize<TElement, TElement>.Deserialize);

        public static ListOperations<TResult, TResultConcrete, TElement, TElement> List<TResult, TResultConcrete, TElement>(this JsonBuilder<TResult, TResultConcrete> builder, Serialize<TElement> serializer, DirectDeserialize<TElement> deserializer)
            where TResultConcrete : TResult
            => builder.List<TResult, TResultConcrete, TElement, TElement>(serializer, deserializer);

        public static ListOperations<TResult, TResultConcrete, Guid, Guid> ListOfGuid<TResult, TResultConcrete>(this JsonBuilder<TResult, TResultConcrete> builder)
            where TResultConcrete : TResult
            => builder.List<TResult, TResultConcrete, Guid, Guid>(Serialize, DeserializeGuid);

        public static ListOperations<TResult, TResultConcrete, int, int> ListOfInt<TResult, TResultConcrete>(this JsonBuilder<TResult, TResultConcrete> builder)
            where TResultConcrete : TResult
            => builder.List<TResult, TResultConcrete, int, int>(Serialize, DeserializeInt);

        public static ListOperations<TResult, TResultConcrete, string, string> ListOfString<TResult, TResultConcrete>(this JsonBuilder<TResult, TResultConcrete> builder)
            where TResultConcrete : TResult
            => builder.List<TResult, TResultConcrete, string, string>(Serialize, DeserializeString);

        public static JsonBuilder<TResult, TResultConcrete> Map<TResult, TResultConcrete, T>(this JsonBuilder<TResult, TResultConcrete> builder, Expression<Getter<TResultConcrete, T>> property, string propertyName = null)
            where TResultConcrete : TResult
            where T : IJsonSerializable<T>, new()
            => builder.Map(property, () => new T(), propertyName);

        public static JsonBuilder<TResult, TResultConcrete> Map<TResult, TResultConcrete>(this JsonBuilder<TResult, TResultConcrete> builder, Expression<Getter<TResultConcrete, bool>> parameter, string propertyName = null)
            where TResultConcrete : TResult
            => builder.Map(parameter, Serialize, DeserializeBool, propertyName);

        public static JsonBuilder<TResult, TResultConcrete> Map<TResult, TResultConcrete>(this JsonBuilder<TResult, TResultConcrete> builder, Expression<Getter<TResultConcrete, DateTime?>> parameter, string propertyName = null)
            where TResultConcrete : TResult
            => builder.Map(parameter, Serialize, DeserializeNullableDateTime, propertyName);

        public static JsonBuilder<TResult, TResultConcrete> Map<TResult, TResultConcrete>(this JsonBuilder<TResult, TResultConcrete> builder, Expression<Getter<TResultConcrete, string>> parameter, string propertyName = null)
            where TResultConcrete : TResult
            => builder.Map(parameter, Serialize, DeserializeString, propertyName);

        public static JsonBuilder<TResult, TResultConcrete> Map<TResult, TResultConcrete>(this JsonBuilder<TResult, TResultConcrete> builder, Expression<Getter<TResultConcrete, int>> parameter, string propertyName = null)
            where TResultConcrete : TResult
            => builder.Map(parameter, Serialize, DeserializeInt, propertyName);

        public static JsonBuilder<TResult, TResultConcrete> Map<TResult, TResultConcrete>(this JsonBuilder<TResult, TResultConcrete> builder, Expression<Getter<TResultConcrete, Guid>> parameter, string propertyName = null)
            where TResultConcrete : TResult
            => builder.Map(parameter, Serialize, DeserializeGuid, propertyName);

        public static JsonBuilder<TResult, TResultConcrete> Map<TResult, TResultConcrete, T, TConcrete>(this JsonBuilder<TResult, TResultConcrete> builder, Expression<Getter<TResultConcrete, T>> property, Serialize<T> serialize, Deserialize<TConcrete> deserialize, Func<TConcrete> itemCreator, string propertyName = null)
            where TResultConcrete : TResult
            where TConcrete : T
        {
            ProcessExpression(property, out Getter<TResultConcrete, T> getter, out Setter<TResultConcrete, T> setter, ref propertyName);
            return builder.MapCore(getter, serialize, deserialize, setter, itemCreator, propertyName);
        }

        public static JsonBuilder<TResult, TResultConcrete> Map<TResult, TResultConcrete, T, TConcrete>(this JsonBuilder<TResult, TResultConcrete> builder, Expression<Getter<TResultConcrete, T>> property, Serialize<T> serialize, Deserialize<TConcrete> deserialize, string propertyName = null)
            where TResultConcrete : TResult
            where TConcrete : T, new()
            => builder.Map(property, serialize, deserialize, () => new TConcrete(), propertyName);

        public static JsonBuilder<TResult, TResultConcrete> Map<TResult, TResultConcrete, T>(this JsonBuilder<TResult, TResultConcrete> builder, Expression<Getter<TResultConcrete, T>> property, Serialize<T> serializer, DirectDeserialize<T> deserializer, string propertyName = null)
            where TResultConcrete : TResult
        {
            ProcessExpression(property, out Getter<TResultConcrete, T> getter, out Setter<TResultConcrete, T> setter, ref propertyName);
            return builder.MapCore(getter, serializer, deserializer, setter, propertyName);
        }

        public static JsonBuilder<TResult, TResultConcrete> Map<TResult, TResultConcrete, T>(this JsonBuilder<TResult, TResultConcrete> builder, Expression<Getter<TResultConcrete, T>> property, Func<T> itemCreator, string propertyName = null)
            where TResultConcrete : TResult
            where T : IJsonSerializable<T>
        {
            JsonSerialize<T>.Configure(itemCreator);
            ProcessExpression(property, out Getter<TResultConcrete, T> getter, out Setter<TResultConcrete, T> setter, ref propertyName);
            return builder.MapCore(getter, JsonSerialize<T>.Serialize, JsonSerialize<T>.Deserialize, setter, propertyName);
        }

        public static IJsonToken NullSerializer<T>(IJsonDocumentObjectModelFactory domFactory, T item) => null;

        public static void ProcessExpression<TResult, T>(Expression<Getter<TResult, T>> property, out Getter<TResult, T> getter, out Setter<TResult, T> setter, ref string propertyName)
        {
            MemberExpression memberExpression = (MemberExpression)property.Body;
            ParameterExpression targetExpression = (ParameterExpression)memberExpression.Expression;
            getter = Expression.Lambda<Getter<TResult, T>>(memberExpression, targetExpression).Compile();

            ParameterExpression valueExpression = Expression.Parameter(typeof(T), "value");
            setter = Expression.Lambda<Setter<TResult, T>>(Expression.Assign(memberExpression, valueExpression), targetExpression, valueExpression).Compile();

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
