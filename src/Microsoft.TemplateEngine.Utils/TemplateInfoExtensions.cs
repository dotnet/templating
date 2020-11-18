using Microsoft.TemplateEngine.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.TemplateEngine.Utils
{
    public static class TemplateInfoExtensions
    {
        public static IEnumerable<string> GetLanguages(this ITemplateInfo template)
        {
            if (template.Tags == null || !template.Tags.TryGetValue("language", out ICacheTag languageTag))
            {
                return new List<string>();
            }
            return languageTag.ChoicesAndDescriptions.Keys.Where(x => !string.IsNullOrWhiteSpace(x));
        }

        public static IEnumerable<string> GetTypes(this ITemplateInfo template)
        {
            if (template.Tags == null || !template.Tags.TryGetValue("type", out ICacheTag typeTag))
            {
                return new List<string>();
            }
            return typeTag.ChoicesAndDescriptions.Keys.Where(x => !string.IsNullOrWhiteSpace(x));
        }
    }
}

