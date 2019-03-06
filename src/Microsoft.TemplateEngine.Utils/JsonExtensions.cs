using System.Runtime.CompilerServices;
using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Utils
{
    public static class JsonExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IJsonArray AddNull(this IJsonArray array) => array.Add(array.Factory.CreateNull());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IJsonArray Add(this IJsonArray array, string value) => array.Add(array.Factory.CreateValue(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IJsonArray Add(this IJsonArray array, int value) => array.Add(array.Factory.CreateValue(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IJsonArray Add(this IJsonArray array, double value) => array.Add(array.Factory.CreateValue(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IJsonArray Add(this IJsonArray array, bool value) => array.Add(array.Factory.CreateValue(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IJsonObject SetValueNull(this IJsonObject obj, string propertyName) => obj.SetValue(propertyName, obj.Factory.CreateNull());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IJsonObject SetValue(this IJsonObject obj, string propertyName, string value) => obj.SetValue(propertyName, obj.Factory.CreateValue(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IJsonObject SetValue(this IJsonObject obj, string propertyName, int value) => obj.SetValue(propertyName, obj.Factory.CreateValue(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IJsonObject SetValue(this IJsonObject obj, string propertyName, double value) => obj.SetValue(propertyName, obj.Factory.CreateValue(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IJsonObject SetValue(this IJsonObject obj, string propertyName, bool value) => obj.SetValue(propertyName, obj.Factory.CreateValue(value));
    }
}
