using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.TemplateEngine.Cli.HelpAndUsage
{
    public class JsonOutputTemplateDetails
    {
        [JsonProperty]
        public string Author { get; set; }

        [JsonProperty]
        public IReadOnlyList<string> Classifications { get; set; }

        [JsonProperty]
        public string Description { get; set; }

        [JsonProperty]
        public string Identity { get; set; }

        [JsonProperty]
        public string GroupIdentity { get; set; }

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public string ShortName { get; set; }

        [JsonProperty]
        public IReadOnlyList<JsonOutputTemplateParameter> Parameters { get; set; }

        [JsonProperty]
        public IReadOnlyList<string> Languages { get; set; }

        [JsonProperty]
        public string Type { get; set; }
    }
}
