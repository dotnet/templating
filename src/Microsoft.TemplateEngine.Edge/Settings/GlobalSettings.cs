using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.GlobalSettings;
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
        public IReadOnlyList<TemplatesSourceData> UserInstalledTemplatesSources
        {
            get
            {
                return userInstalledTemplatesSources;
            }
            set
            {
                userInstalledTemplatesSources = new List<TemplatesSourceData>(value);
                SettingsChanged?.Invoke();
            }
        }

        private List<TemplatesSourceData> userInstalledTemplatesSources = new List<TemplatesSourceData>();

        // What this does, if older TemplateEngine loads this file and save it back
        // it will include new settings that new TemplateEngine depends on
        // without this field, data would be lost in process of loading and saving
        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        public event Action SettingsChanged;

        public void Add(TemplatesSourceData userInstalledTemplate)
        {
            userInstalledTemplatesSources.Add(userInstalledTemplate);
            SettingsChanged?.Invoke();
        }

        public void Remove(TemplatesSourceData userInstalledTemplate)
        {
            userInstalledTemplatesSources.Remove(userInstalledTemplate);
            SettingsChanged?.Invoke();
        }
    }
}
