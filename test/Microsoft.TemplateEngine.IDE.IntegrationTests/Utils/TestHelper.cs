using System;
using System.IO;
using System.Reflection;

namespace Microsoft.TemplateEngine.IDE.IntegrationTests.Utils
{
    internal static class TestHelper
    {
        internal static string CreateTemporaryFolder(string name = "")
        {
            string workingDir = Path.Combine(Path.GetTempPath(), "IDE.IntegrationTests", Guid.NewGuid().ToString(), name);
            Directory.CreateDirectory(workingDir);
            return workingDir;
        }

        internal static string GetTestTemplateLocation(string templateName)
        {
            string codebase = typeof(TestHelper).GetTypeInfo().Assembly.Location;
            string dir = Path.GetDirectoryName(codebase);
            string templateLocation = Path.Combine(dir, "..", "..", "..", "..", "..", "test", "Microsoft.TemplateEngine.TestTemplates", "test_templates", templateName);

            if (!Directory.Exists(templateLocation))
            {
                throw new Exception($"{templateLocation} does not exist");
            }
            return Path.GetFullPath(templateLocation);
        }

    }
}
