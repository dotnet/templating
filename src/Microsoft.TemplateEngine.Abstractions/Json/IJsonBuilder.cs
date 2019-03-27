namespace Microsoft.TemplateEngine.Abstractions.Json
{
    public interface IJsonBuilder<T>
    {
        T Deserialize(IJsonToken source);

        IJsonObject Serialize(IJsonDocumentObjectModelFactory domFactory, T item);
    }
}
