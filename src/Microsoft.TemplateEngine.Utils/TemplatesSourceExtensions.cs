using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TemplateEngine.Utils
{
    public static class TemplatesSourceExtensions
    {
        public static IEnumerable<ITemplateInfo> GetTemplates (this ITemplatesSource templateSource, IEngineEnvironmentSettings settings)
        {
            HashSet<ITemplateInfo> templateCache = new HashSet<ITemplateInfo>();
            settings.SettingsLoader.GetTemplates(templateCache);
            return templateCache.Where(t => t.MountPointUri == templateSource.MountPointUri);
        }
    }
}
