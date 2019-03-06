namespace Microsoft.TemplateEngine.Abstractions.Json
{
    public interface IJsonDocumentObjectModelFactory
    {
        bool TryParse(string jsonText, out IJsonToken root);

        IJsonObject CreateObject();

        IJsonArray CreateArray();

        IJsonValue CreateValue(int value);

        IJsonValue CreateValue(double value);

        IJsonValue CreateValue(string value);

        IJsonValue CreateValue(bool value);

        IJsonValue CreateNull();
    }
}
