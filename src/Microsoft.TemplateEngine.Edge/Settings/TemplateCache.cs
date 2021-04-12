using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Template;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.TemplateEngine.Edge.Settings
{
    internal class TemplateCache
    {
        internal const string CurrentVersion = "1.0.0.5";

        private IDictionary<string, ITemplate> _templateMemoryCache = new Dictionary<string, ITemplate>();

        // locale -> identity -> locator
        private readonly IDictionary<string, IDictionary<string, ILocalizationLocator>> _localizationMemoryCache = new Dictionary<string, IDictionary<string, ILocalizationLocator>>();
        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly Paths _paths;

        public TemplateCache(IEngineEnvironmentSettings environmentSettings)
        {
            _environmentSettings = environmentSettings;
            _paths = new Paths(environmentSettings);
            TemplateInfo = new List<TemplateInfo>();
            MountPointsInfo = new Dictionary<string, DateTime>();
            Locale = CultureInfo.CurrentUICulture.Name;
            Version = CurrentVersion;
            InstallScanner = new Scanner(environmentSettings);
        }

        public TemplateCache(IEngineEnvironmentSettings environmentSettings, JObject parsed)
            : this(environmentSettings)
        {
            ParseCacheContent(parsed);
        }

        [JsonProperty]
        public string Version { get; private set; }

        [JsonProperty]
        public string Locale { get; private set; }

        [JsonProperty]
        public IReadOnlyList<TemplateInfo> TemplateInfo { get; set; }

        [JsonProperty]
        public Dictionary<string, DateTime> MountPointsInfo { get; set; }

        // This method is getting obsolted soon. It's getting replaced by TemplateListFilter.FilterTemplates, which does the same thing,
        // except that the template list to act on is passed in.
        public IReadOnlyCollection<IFilteredTemplateInfo> List(bool exactMatchesOnly, params Func<ITemplateInfo, MatchInfo?>[] filters)
        {
            return TemplateListFilter.FilterTemplates(TemplateInfo, exactMatchesOnly, filters);
        }

        private Scanner InstallScanner { get; }

        private void AddTemplatesAndLangPacksFromScanResult(ScanResult scanResult)
        {
            foreach (ILocalizationLocator locator in scanResult.Localizations)
            {
                AddLocalizationToMemoryCache(locator);
            }

            foreach (ITemplate template in scanResult.Templates)
            {
                AddTemplateToMemoryCache(template);
            }
        }

        public void Scan(string installDir)
        {
            ScanResult scanResult = InstallScanner.Scan(installDir);
            AddTemplatesAndLangPacksFromScanResult(scanResult);
        }

        private void ParseCacheContent(JObject contentJobject)
        {
            if (contentJobject.TryGetValue(nameof(Version), StringComparison.OrdinalIgnoreCase, out JToken versionToken))
            {
                Version = versionToken.ToString();

                if (Version != CurrentVersion)
                {
                    _environmentSettings.Host.LogDiagnosticMessage(
                        $"Template cache file version is {Version}, but template engine is {CurrentVersion}, rebuilding cache.",
                        "Debug");
                    return;
                }
            }
            else
            {
                Version = string.Empty;
                return;
            }

            if (contentJobject.TryGetValue(nameof(Locale), StringComparison.OrdinalIgnoreCase, out JToken localeToken))
            {
                Locale = localeToken.ToString();
            }


            var mountPointInfo = new Dictionary<string, DateTime>();

            if (contentJobject.TryGetValue(nameof(MountPointsInfo), StringComparison.OrdinalIgnoreCase, out JToken mountPointInfoToken))
            {
                if (mountPointInfoToken is IDictionary<string, JToken> dict)
                {
                    foreach (var entry in dict)
                    {
                        mountPointInfo.Add(entry.Key, entry.Value.ToObject<DateTime>());
                    }
                }
            }

            MountPointsInfo = mountPointInfo;

            List<TemplateInfo> templateList = new List<TemplateInfo>();

            if (contentJobject.TryGetValue(nameof(TemplateInfo), StringComparison.OrdinalIgnoreCase, out JToken templateInfoToken))
            {
                if (templateInfoToken is JArray arr)
                {
                    foreach (JToken entry in arr)
                    {
                        if (entry != null && entry.Type == JTokenType.Object)
                        {
                            templateList.Add(new TemplateInfo((JObject)entry));
                        }
                    }
                }
            }

            TemplateInfo = templateList;
        }

        public void DeleteAllLocaleCacheFiles()
        {
            _paths.Delete(_paths.User.TemplateCacheFile);
        }

        public void WriteTemplateCaches(Dictionary<string, DateTime> mountPoints)
        {
            bool hasContentChanges = false;

            HashSet<string> foundTemplates = new HashSet<string>();
            List<TemplateInfo> mergedTemplateList = new List<TemplateInfo>();

            // These are from langpacks being installed... identity -> locator
            if (string.IsNullOrEmpty(Locale)
                || !_localizationMemoryCache.TryGetValue(Locale, out IDictionary<string, ILocalizationLocator> newLocatorsForLocale))
            {
                newLocatorsForLocale = new Dictionary<string, ILocalizationLocator>();
            }
            else
            {
                hasContentChanges = true;   // there are new langpacks for this locale
            }

            foreach (ITemplate newTemplate in _templateMemoryCache.Values)
            {
                ILocalizationLocator? locatorForTemplate = GetPreferredLocatorForTemplate(newTemplate.Identity, newLocatorsForLocale);
                TemplateInfo localizedTemplate = new TemplateInfo(newTemplate, locatorForTemplate);
                mergedTemplateList.Add(localizedTemplate);
                foundTemplates.Add(newTemplate.Identity);

                hasContentChanges = true;   // new template
            }

            foreach (TemplateInfo existingTemplate in TemplateInfo)
            {
                if (!foundTemplates.Contains(existingTemplate.Identity))
                {
                    mergedTemplateList.Add(existingTemplate);
                    foundTemplates.Add(existingTemplate.Identity);
                }
            }
            WriteTemplateCache(mountPoints, mergedTemplateList, hasContentChanges);
        }

        private void WriteTemplateCache(Dictionary<string, DateTime> mountPoints, List<TemplateInfo> templates, bool hasContentChanges)
        {
            bool hasMountPointChanges = false;

            for (int i = 0; i < templates.Count; ++i)
            {
                if (!mountPoints.ContainsKey(templates[i].MountPointUri))
                {
                    templates.RemoveAt(i);
                    --i;
                    hasMountPointChanges = true;
                }
            }

            this.Version = CurrentVersion;
            this.TemplateInfo = templates;
            this.MountPointsInfo = mountPoints;

            if (hasContentChanges || hasMountPointChanges)
            {
                JObject serialized = JObject.FromObject(this);
                _paths.WriteAllText(_paths.User.TemplateCacheFile, serialized.ToString());
            }
        }

        private ILocalizationLocator? GetPreferredLocatorForTemplate(string identity, IDictionary<string, ILocalizationLocator> newLocatorsForLocale)
        {
            if (newLocatorsForLocale.TryGetValue(identity, out ILocalizationLocator locatorForTemplate))
            {
                return locatorForTemplate;
            }
            return null;
        }

        // Adds the template to the memory cache, keyed on identity.
        // If the identity is the same as an existing one, it's overwritten.
        // (last in wins)
        private void AddTemplateToMemoryCache(ITemplate template)
        {
            _templateMemoryCache[template.Identity] = template;
        }

        private void AddLocalizationToMemoryCache(ILocalizationLocator locator)
        {
            if (!_localizationMemoryCache.TryGetValue(locator.Locale, out IDictionary<string, ILocalizationLocator> localeLocators))
            {
                localeLocators = new Dictionary<string, ILocalizationLocator>();
                _localizationMemoryCache.Add(locator.Locale, localeLocators);
            }

            localeLocators[locator.Identity] = locator;
        }
    }
}
