using Microsoft.TemplateEngine.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    public class GlobalSettings : IGlobalSettings
    {
        [JsonProperty]
        public string DefaultLanguage { get; set; }

        [JsonProperty]
        public List<UserInstalledTemplatesSource> UserInstalledTemplatesSources { get; } = new List<UserInstalledTemplatesSource>();

        // What this does, if older TemplateEngine loads this file and stores it back
        // it will include new settings that new TemplateEngine depends on
        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;
    }
}
