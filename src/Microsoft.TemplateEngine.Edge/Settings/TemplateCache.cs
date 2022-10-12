// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    internal class TemplateCache
    {
        private static readonly Guid RunnableProjectGeneratorId = new("0C434DF7-E2CB-4DEE-B216-D7C58C8EB4B3");

        public TemplateCache(ScanResult?[] scanResults, Dictionary<string, DateTime> mountPoints, IEngineEnvironmentSettings environmentSettings)
        {
            // We need this dictionary to de-duplicate templates that have same identity
            // notice that IEnumerable<ScanResult> that we get in is order by priority which means
            // last template with same Identity wins, others are ignored...
            var templateDeduplicationDictionary = new Dictionary<string, (IMountPoint, IScanTemplateInfo)>();
            foreach (ScanResult? scanResult in scanResults)
            {
                if (scanResult == null)
                {
                    continue;
                }
                foreach (IScanTemplateInfo template in scanResult.Templates)
                {
                    templateDeduplicationDictionary[template.Identity] = (scanResult.MountPoint, template);
                }
            }

            var templates = new List<TemplateInfo>();
            foreach ((IMountPoint mountPoint, IScanTemplateInfo newTemplate) in templateDeduplicationDictionary.Values)
            {
                ILocalizationLocator? loc = GetBestLocalizationLocatorMatch(newTemplate);
                (string, JObject?)? hostFile = GetBestHostConfigMatch(newTemplate, environmentSettings, mountPoint);

                templates.Add(new TemplateInfo(newTemplate, loc, hostFile));
            }

            Version = Settings.TemplateInfo.CurrentVersion;
            Locale = CultureInfo.CurrentUICulture.Name;
            TemplateInfo = templates;
            MountPointsInfo = mountPoints;
        }

        public TemplateCache(JObject? contentJobject)
        {
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

        private ILocalizationLocator? GetBestLocalizationLocatorMatch(IScanTemplateInfo template)
        {
            if (template.Localizations is null)
            {
                return null;
            }

            if (!template.Localizations.Any())
            {
                return null;
            }

            string? bestMatch = GetBestLocaleMatch(template.Localizations.Keys);
            if (string.IsNullOrWhiteSpace(bestMatch))
            {
                return null;
            }
            return template.Localizations[bestMatch!];
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

        private (string, JObject?)? GetBestHostConfigMatch(IScanTemplateInfo newTemplate, IEngineEnvironmentSettings settings, IMountPoint mountPoint)
        {
            if (newTemplate.HostConfigFiles.TryGetValue(settings.Host.HostIdentifier, out string? preferredHostFilePath))
            {
                return (preferredHostFilePath, ReadHostFile(newTemplate, preferredHostFilePath, settings, mountPoint));
            }

            foreach (string fallbackHostName in settings.Host.FallbackHostTemplateConfigNames)
            {
                if (newTemplate.HostConfigFiles.TryGetValue(fallbackHostName, out string? fallbackHostFilePAth))
                {
                    return (preferredHostFilePath, ReadHostFile(newTemplate, fallbackHostFilePAth, settings, mountPoint));
                }
            }
            return null;
        }

        private JObject? ReadHostFile(IScanTemplateInfo template, string path, IEngineEnvironmentSettings settings, IMountPoint mountPoint)
        {
            if (template.GeneratorId != RunnableProjectGeneratorId)
            {
                return null;
            }
            settings.Host.Logger.LogDebug($"Start loading host config {template.MountPointUri}{path}");
            try
            {
                IFile? hostFile = mountPoint.FileInfo(path);
                if (hostFile == null || !hostFile.Exists)
                {
                    throw new FileNotFoundException($"Host file '{hostFile?.GetDisplayPath()}' does not exist.");
                }
                return hostFile.ReadJObjectFromIFile();
            }
            catch (Exception e)
            {
                settings.Host.Logger.LogWarning(
                    e,
                    LocalizableStrings.TemplateInfo_Warning_FailedToReadHostData,
                    template.MountPointUri,
                    path);
            }
            finally
            {
                settings.Host.Logger.LogDebug($"End loading host config {template.MountPointUri}{path}");
            }
            return null;
        }

    }
}
