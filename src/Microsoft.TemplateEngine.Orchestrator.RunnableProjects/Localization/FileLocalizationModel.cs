﻿using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Localization
{
    public class FileLocalizationModel : IFileLocalizationModel
    {
        public string File { get; set; }

        // original -> localized
        public IReadOnlyDictionary<string, string> Localizations { get; set; }

        public static FileLocalizationModel FromJObject(string fileName, JObject fileSection)
        {
            Dictionary<string, string> localizations = new Dictionary<string, string>();

            foreach (JObject entry in fileSection.Items<JObject>("localizations"))
            {
                string original = entry.ToString("original");
                string localized = entry.ToString("localization");
                localizations.Add(original, localized);
            }

            FileLocalizationModel model = new FileLocalizationModel()
            {
                File = fileName,
                Localizations = localizations
            };

            return model;
        }
    }
}
