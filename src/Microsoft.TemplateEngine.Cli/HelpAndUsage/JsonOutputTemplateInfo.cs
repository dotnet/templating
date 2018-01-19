using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.TemplateEngine.Cli.HelpAndUsage
{
    public class JsonOutputTemplateInfo
    {
        [JsonProperty]
        public string GroupIndentity { get; set; }

        [JsonProperty]
        public string Identity { get; set; }

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public string ShortName { get; set; }

        [JsonProperty]
        public string Language { get; set; }

        [JsonProperty]
        public IReadOnlyList<string> Classifications { get; set; }
    }
}
