using System;
using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Utils.Json
{
    internal class JsonSerialize<T>
        where T : IJsonSerializable<T>
    {
        public static Lazy<T> _instance = new Lazy<T>(() => (T)Activator.CreateInstance(typeof(T)));

        public static void Configure(Func<T> creator) => _instance = _instance ?? new Lazy<T>(creator);

        public static T Deserialize(IJsonToken token) => _instance.Value.JsonBuilder.Deserialize(token);

        public static IJsonToken Serialize(IJsonDocumentObjectModelFactory domFactory, T instance) => _instance.Value.JsonBuilder.Serialize(domFactory, instance);
    }

    internal class JsonSerialize<T, TConcrete>
        where T : IJsonSerializable<T>
        where TConcrete : T, new()
    {
        private static readonly TConcrete _instance = new TConcrete();

        public static TConcrete Deserialize(IJsonToken token) => (TConcrete)_instance.JsonBuilder.Deserialize(token);

        public static IJsonToken Serialize(IJsonDocumentObjectModelFactory domFactory, T instance) => _instance.JsonBuilder.Serialize(domFactory, instance);
    }
}
