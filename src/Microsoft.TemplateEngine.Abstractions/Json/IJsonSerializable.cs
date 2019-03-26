namespace Microsoft.TemplateEngine.Abstractions.Json
{
    public interface IJsonSerializable<T>
    {
        IJsonBuilder<T> JsonBuilder { get; }
    }
}
