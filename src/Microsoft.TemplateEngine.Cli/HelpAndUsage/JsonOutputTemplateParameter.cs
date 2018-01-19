using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Cli.HelpAndUsage
{
    public class JsonOutputTemplateParameter
    {
        [JsonProperty]
        public string Name { get; set; }
        
        [JsonProperty]
        public IReadOnlyList<string> CliOptionVariants { get; set; }

        [JsonProperty]
        public string DataType { get; set; }

        [JsonProperty]
        public string Description { get; set; }

        [JsonProperty]
        public string DefaultValue { get; set; }

        [JsonProperty]
        public IReadOnlyDictionary<string, string> ChoicesAndDescriptions { get; set; }

        public bool ShouldSerializeChoicesAndDescriptions()
        {
            return string.Equals(DataType, "choice", StringComparison.Ordinal);
        }
    }
}
