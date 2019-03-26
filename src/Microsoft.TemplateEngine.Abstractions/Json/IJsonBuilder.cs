namespace Microsoft.TemplateEngine.Abstractions.Json
{
    public interface IJsonBuilder<TResult>
    {
        TResult Deserialize(IJsonToken source);

        IJsonObject Serialize(IJsonDocumentObjectModelFactory domFactory, TResult item);
    }
}
