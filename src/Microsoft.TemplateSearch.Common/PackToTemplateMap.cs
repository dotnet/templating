using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.TemplateSearch.Common
{
    public class PackToTemplateMap
    {
        public PackToTemplateMap(IReadOnlyDictionary<string, PackToTemplateEntry> templatesInPacks)
        {
            TemplatesInPacks = templatesInPacks;
        }

        [JsonProperty]
        public IReadOnlyDictionary<string, PackToTemplateEntry> TemplatesInPacks { get; }
    }
}
