using System.IO;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Json;
using Microsoft.TemplateEngine.Abstractions.Mount;

namespace Microsoft.TemplateEngine.Cli
{
    public class HostSpecificDataLoader : IHostSpecificDataLoader
    {
        public HostSpecificDataLoader(ISettingsLoader settingsLoader)
        {
            _settingsLoader = settingsLoader;
        }

        private ISettingsLoader _settingsLoader;

        public HostSpecificTemplateData ReadHostSpecificTemplateData(ITemplateInfo templateInfo)
        {
            IMountPoint mountPoint = null;

            try
            {
                if (_settingsLoader.TryGetFileFromIdAndPath(templateInfo.HostConfigMountPointId, templateInfo.HostConfigPlace, out IFile file, out mountPoint))
                {
                    using (Stream stream = file.OpenRead())
                    using (TextReader textReader = new StreamReader(stream, true))
                    {
                        string jsonText = textReader.ReadToEnd();
                        if (!_settingsLoader.EnvironmentSettings.JsonDomFactory.TryParse(jsonText, out IJsonToken root))
                        {
                            return HostSpecificTemplateData.Default;
                        }

                        return HostSpecificTemplateData.DeserializationPlan.Deserialize(root);
                    }
                }
            }
            catch
            {
                // ignore malformed host files
            }
            finally
            {
                if (mountPoint != null)
                {
                    _settingsLoader.ReleaseMountPoint(mountPoint);
                }
            }

            return HostSpecificTemplateData.Default;
        }
    }
}
