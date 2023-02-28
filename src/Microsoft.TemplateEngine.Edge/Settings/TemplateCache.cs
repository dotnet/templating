// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatePackage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    internal class TemplateCache
    {
        private const string BulletSymbol = "\u2022";

        private readonly ILogger _logger;

        public TemplateCache(IReadOnlyList<ITemplatePackage> allTemplatePackages, ScanResult?[] scanResults, Dictionary<string, DateTime> mountPoints, ILogger logger)
        {
            _logger = logger;

            // this dictionary contains information about managed templates with overlapping identities
            var overlappingIdentitiesMap = new Dictionary<DuplicatedIdentity, IList<(string TemplateName, string PackageId)>>(new DuplicatedIdentityComparer());

            // We need this dictionary to de-duplicate templates that have same identity
            // notice that IEnumerable<ScanResult> that we get in is order by priority which means
            // last template with same Identity wins, others are ignored...
            var templateDeduplicationDictionary = new Dictionary<string, (ITemplate Template, ILocalizationLocator? Localization)>();
            foreach (var scanResult in scanResults)
            {
                if (scanResult == null)
                {
                    continue;
                }

                foreach (ITemplate template in scanResult.Templates)
                {
                    if (templateDeduplicationDictionary.ContainsKey(template.Identity))
                    {
                        // add data to overlappingIdentitiesMap if there is an attempt to overwrite existing managed by new managed template
                        CheckForOverlappingIdentity(overlappingIdentitiesMap, template, templateDeduplicationDictionary[template.Identity].Template, allTemplatePackages);
                    }

                    templateDeduplicationDictionary[template.Identity] = (template, GetBestLocalizationLocatorMatch(scanResult.Localizations, template.Identity));
                }
            }

            var templates = new List<TemplateInfo>();
            foreach (var newTemplate in templateDeduplicationDictionary.Values)
            {
                templates.Add(new TemplateInfo(newTemplate.Template, newTemplate.Localization, logger));
            }

            Version = Settings.TemplateInfo.CurrentVersion;
            Locale = CultureInfo.CurrentUICulture.Name;
            TemplateInfo = templates;
            MountPointsInfo = mountPoints;

            PrintOverlappingIdentityWarning(overlappingIdentitiesMap);
        }

        public TemplateCache(JObject? contentJobject, ILogger logger)
        {
            _logger = logger;
            if (contentJobject != null && contentJobject.TryGetValue(nameof(Version), StringComparison.OrdinalIgnoreCase, out JToken? versionToken))
            {
                Version = versionToken.ToString();
            }
            else
            {
                Version = null;
                TemplateInfo = Array.Empty<TemplateInfo>();
                MountPointsInfo = new Dictionary<string, DateTime>();
                Locale = string.Empty;
                return;
            }

            Locale = contentJobject.TryGetValue(nameof(Locale), StringComparison.OrdinalIgnoreCase, out JToken? localeToken)
                ? localeToken.ToString()
                : string.Empty;

            var mountPointInfo = new Dictionary<string, DateTime>();

            if (contentJobject.TryGetValue(nameof(MountPointsInfo), StringComparison.OrdinalIgnoreCase, out JToken? mountPointInfoToken) && mountPointInfoToken is IDictionary<string, JToken> dict)
            {
                foreach (var entry in dict)
                {
                    mountPointInfo.Add(entry.Key, entry.Value.Value<DateTime>());
                }
            }

            MountPointsInfo = mountPointInfo;

            List<TemplateInfo> templateList = new List<TemplateInfo>();

            if (contentJobject.TryGetValue(nameof(TemplateInfo), StringComparison.OrdinalIgnoreCase, out JToken? templateInfoToken) && templateInfoToken is JArray arr)
            {
                foreach (JToken entry in arr)
                {
                    if (entry != null && entry.Type == JTokenType.Object)
                    {
                        templateList.Add(Settings.TemplateInfo.FromJObject((JObject)entry));
                    }
                }
            }

            TemplateInfo = templateList;
        }

        [JsonProperty]
        public string? Version { get; }

        [JsonProperty]
        public string Locale { get; }

        [JsonProperty]
        public IReadOnlyList<TemplateInfo> TemplateInfo { get; }

        [JsonProperty]
        public Dictionary<string, DateTime> MountPointsInfo { get; }

        private ILocalizationLocator? GetBestLocalizationLocatorMatch(IReadOnlyList<ILocalizationLocator> localizations, string identity)
        {
            IEnumerable<ILocalizationLocator> localizationsForTemplate = localizations.Where(locator => locator.Identity.Equals(identity, StringComparison.OrdinalIgnoreCase));

            if (!localizations.Any())
            {
                return null;
            }
            IEnumerable<string> availableLocalizations = localizationsForTemplate.Select(locator => locator.Locale);
            string? bestMatch = GetBestLocaleMatch(availableLocalizations);
            if (string.IsNullOrWhiteSpace(bestMatch))
            {
                return null;
            }
            return localizationsForTemplate.FirstOrDefault(locator => locator.Locale == bestMatch);
        }

        /// <remarks>see https://source.dot.net/#System.Private.CoreLib/ResourceFallbackManager.cs.</remarks>
        private string? GetBestLocaleMatch(IEnumerable<string> availableLocalizations)
        {
            CultureInfo currentCulture = CultureInfo.CurrentUICulture;
            do
            {
                if (availableLocalizations.Contains(currentCulture.Name, StringComparer.OrdinalIgnoreCase))
                {
                    return currentCulture.Name;
                }
                currentCulture = currentCulture.Parent;
            }
            while (currentCulture.Name != CultureInfo.InvariantCulture.Name);
            return null;
        }

        private void CheckForOverlappingIdentity(
            IDictionary<DuplicatedIdentity, IList<(string TemplateName, string PackageId)>> overlappingIdentitiesMap,
            ITemplate template,
            ITemplate checkedTemplate,
            IReadOnlyList<ITemplatePackage> allTemplatePackages)
        {
            var templatePackage = allTemplatePackages.FirstOrDefault(tp => tp.MountPointUri == template.MountPointUri);
            var checkedTemplatePackage = allTemplatePackages.FirstOrDefault(tp => tp.MountPointUri == checkedTemplate.MountPointUri);
            var isTemplatePackageManaged = templatePackage is IManagedTemplatePackage;
            var checkedTPPath = checkedTemplatePackage is IManagedTemplatePackage checkedManagedTP ? checkedManagedTP.DisplayName : checkedTemplatePackage.MountPointUri;

            var duplicatedIdentity = new DuplicatedIdentity(
                template.Identity,
                templatePackage is IManagedTemplatePackage managedTP ? managedTP.DisplayName : string.Empty,
                isTemplatePackageManaged);
            if (overlappingIdentitiesMap.ContainsKey(duplicatedIdentity))
            {
                // need to substitute key due to changes in template path
                // since the key uniqueness is defined by identity only
                // see DuplicatedIdentityComparer
                var values = overlappingIdentitiesMap[duplicatedIdentity];
                values.Add((checkedTemplate.Name, checkedTPPath));

                overlappingIdentitiesMap.Remove(duplicatedIdentity);
                overlappingIdentitiesMap.Add(duplicatedIdentity, values);
            }
            else
            {
                overlappingIdentitiesMap.Add(duplicatedIdentity, new List<(string TemplateName, string PackageId)> { (checkedTemplate.Name, checkedTPPath) });
            }
        }

        // add warning for the case when there is an attempt to overwrite existing managed by new managed template
        private void PrintOverlappingIdentityWarning(IDictionary<DuplicatedIdentity, IList<(string TemplateName, string PackageId)>> overlappingIdentitiesMap)
        {
            foreach (var identityTemplates in overlappingIdentitiesMap)
            {
                // we print the message only if managed template wins
                if (identityTemplates.Key.IsManagedTemplatePackage)
                {
                    var templatesList = new StringBuilder();
                    foreach (var (templateName, packageId) in identityTemplates.Value)
                    {
                        templatesList.AppendLine(string.Format(
                            LocalizableStrings.TemplatePackageManager_Warning_DetectedTemplatesIdentityConflict_Subentry,
                            BulletSymbol,
                            templateName,
                            packageId));
                    }

                    _logger.LogWarning(string.Format(
                            LocalizableStrings.TemplatePackageManager_Warning_DetectedTemplatesIdentityConflict,
                            identityTemplates.Key.Identity,
                            templatesList.ToString().TrimEnd(Environment.NewLine.ToCharArray()),
                            identityTemplates.Key.PackageId));
                }
            }
        }

        private class DuplicatedIdentity
        {
            public DuplicatedIdentity(string identity, string packageId, bool isManagedTemplatePackage)
            {
                Identity = identity;
                PackageId = packageId;
                IsManagedTemplatePackage = isManagedTemplatePackage;
            }

            public string Identity { get; set; }

            public string PackageId { get; set; }

            public bool IsManagedTemplatePackage { get; set; }
        }

        private class DuplicatedIdentityComparer : IEqualityComparer<DuplicatedIdentity>
        {
            public bool Equals(DuplicatedIdentity x, DuplicatedIdentity y) => x.Identity == y.Identity;

            public int GetHashCode(DuplicatedIdentity x) => x.Identity.GetHashCode();
        }
    }
}
