using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions.Json
{
    public interface IJsonDeserializationBuilder<out T>
    {
        T Deserialize(IJsonToken source);

        T Deserialize(IEnumerable<KeyValuePair<string, IJsonToken>> stashedTokens, IEnumerator<KeyValuePair<string, IJsonToken>> remainingProperties);
    }

    public interface IJsonSerializationBuilder<in T>
    {
        IJsonObject Serialize(IJsonDocumentObjectModelFactory domFactory, T item);
    }

    public interface IJsonBuilder<T> : IJsonDeserializationBuilder<T>, IJsonSerializationBuilder<T>
    {
    }
}
