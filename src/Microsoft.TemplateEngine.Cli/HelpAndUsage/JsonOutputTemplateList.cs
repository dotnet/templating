using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.TemplateEngine.Cli.HelpAndUsage
{
    public class JsonOutputTemplateList
    {
        public JsonOutputTemplateList(IReadOnlyList<JsonOutputTemplateInfo> templates)
        {
            Templates = templates;
        }

        [JsonProperty]
        public IReadOnlyList<JsonOutputTemplateInfo> Templates { get; }
    }
}
