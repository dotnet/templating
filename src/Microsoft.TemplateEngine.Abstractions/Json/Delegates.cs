using System;

namespace Microsoft.TemplateEngine.Abstractions.Json
{
    public delegate T Deserialize<T>(IJsonToken token, Func<T> itemCreator);

    public delegate T DirectDeserialize<T>(IJsonToken token);

    public delegate TValue Getter<T, TValue>(T source);

    public delegate IJsonToken Serialize<T>(IJsonDocumentObjectModelFactory domFactory, T item);

    public delegate void Setter<T, TValue>(T source, TValue value);

    public delegate T Chain<T>(T item);
}
