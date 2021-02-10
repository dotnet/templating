// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.GlobalSettings;
using Newtonsoft.Json;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    public class GlobalSettings : IGlobalSettings
    {
        [JsonProperty("UserInstalledTemplatesSources")]
        private List<TemplatesSourceData> _userInstalledTemplatesSources = new List<TemplatesSourceData>();

        public event Action SettingsChanged;

        [JsonProperty]
        public string DefaultLanguage { get; set; }

        [JsonIgnore]
        public IReadOnlyList<TemplatesSourceData> UserInstalledTemplatesSources => _userInstalledTemplatesSources;

        // What this does, if older TemplateEngine loads this file and save it back
        // it will include new settings that new TemplateEngine depends on
        // without this field, data would be lost in process of loading and saving
        //[JsonExtensionData]
        //private IDictionary<string, JToken> _additionalData;
        public void Add(TemplatesSourceData userInstalledTemplate)
        {
            _userInstalledTemplatesSources.RemoveAll(data => data.MountPointUri == userInstalledTemplate.MountPointUri);
            _userInstalledTemplatesSources.Add(userInstalledTemplate);
            SettingsChanged?.Invoke();
        }

        public void Remove(TemplatesSourceData userInstalledTemplate)
        {
            _userInstalledTemplatesSources.RemoveAll(data => data.MountPointUri == userInstalledTemplate.MountPointUri);
            SettingsChanged?.Invoke();
        }
    }
}
