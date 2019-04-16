using System.IO;

namespace Microsoft.TemplateEngine.Abstractions.Json
{
    public interface IJsonToken
    {
        JsonTokenType TokenType { get; }

        void WriteToStream(Stream s);

        IJsonDocumentObjectModelFactory Factory { get; }

        IJsonToken Clone();
    }
}
