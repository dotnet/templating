namespace Microsoft.TemplateEngine.Abstractions.Json
{
    public interface IJsonValue : IJsonToken
    {
        object Value { get; }
    }
}
