using System.IO;
using System.Text;
using Microsoft.TemplateEngine.Abstractions.Json;
using Microsoft.TemplateEngine.Abstractions.Mount;

namespace Microsoft.TemplateEngine.Utils
{
    public static class JsonDocumentObjectModelFactoryExtensions
    {
        public static bool TryLoadFromFile(this IJsonDocumentObjectModelFactory jsonDomFactory, string pathToFile, out IJsonToken jsonToken)
        {
            string text;

            using (Stream s = File.OpenRead(pathToFile))
            using (TextReader r = new StreamReader(s, Encoding.UTF8, true, 4096))
            {
                text = r.ReadToEnd();
            }

            return jsonDomFactory.TryParse(text, out jsonToken);
        }

        public static bool TryLoadFromFile(this IJsonDocumentObjectModelFactory jsonDomFactory, IFile file, out IJsonToken jsonToken)
        {
            string text;

            using (Stream stream = file.OpenRead())
            using (TextReader reader = new StreamReader(stream, Encoding.UTF8, true, 4096))
            {
                text = reader.ReadToEnd();
            }

            return jsonDomFactory.TryParse(text, out jsonToken);
        }
    }
}
