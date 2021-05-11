// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateSearch.Common
{
    public static class FileMetadataTemplateSearchCacheReader
    {
        public static bool TryReadDiscoveryMetadata(IEngineEnvironmentSettings environmentSettings, ISearchCacheConfig config, out TemplateDiscoveryMetadata discoveryMetadata)
        {
            string pathToConfig = Path.Combine(environmentSettings.Paths.HostVersionSettingsDir, config.TemplateDiscoveryFileName);
            string cacheText = environmentSettings.Host.FileSystem.ReadAllText(pathToConfig);

            if (TryReadDiscoveryMetadata(environmentSettings, cacheText, config, out discoveryMetadata))
            {
                return true;
            }
            discoveryMetadata = null;
            return false;
        }

        public static bool TryReadDiscoveryMetadata(IEngineEnvironmentSettings environmentSettings, string cacheText, ISearchCacheConfig config, out TemplateDiscoveryMetadata discoveryMetadata)
        {
            JObject cacheObject = JObject.Parse(cacheText);

            // add the reader calls, build the model objects
            if (TryReadVersion(cacheObject, out string version)
                && TryReadTemplateList(environmentSettings, cacheObject, version, out IReadOnlyList<ITemplateInfo> templateList)
                && TryReadPackToTemplateMap(cacheObject, out IReadOnlyDictionary<string, PackToTemplateEntry> packToTemplateMap)
                && TryReadAdditionalData(cacheObject, config.AdditionalDataReaders, out IReadOnlyDictionary<string, object> additionalDta))
            {
                discoveryMetadata = new TemplateDiscoveryMetadata(version, templateList, packToTemplateMap, additionalDta);
                return true;
            }

            discoveryMetadata = null;
            return false;
        }

        private static bool TryReadVersion(JObject cacheObject, out string version)
        {
            if (cacheObject.TryGetValue(nameof(TemplateDiscoveryMetadata.Version), out JToken value))
            {
                version = value.Value<string>();
                return true;
            }

            version = null;
            return false;
        }

        private static bool TryReadTemplateList(
            IEngineEnvironmentSettings environmentSettings,
            JObject cacheObject,
            string cacheVersion,
            out IReadOnlyList<ITemplateInfo> templateList)
        {
            try
            {
                // This is lifted from TemplateCache.ParseCacheContent - almost identical
                if (cacheObject.TryGetValue(nameof(TemplateDiscoveryMetadata.TemplateCache), StringComparison.OrdinalIgnoreCase, out JToken templateInfoToken))
                {
                    List<ITemplateInfo> buildingTemplateList = new List<ITemplateInfo>();

                    if (templateInfoToken is JArray arr)
                    {
                        foreach (JToken entry in arr)
                        {
                            if (entry != null && entry.Type == JTokenType.Object)
                            {
                                try
                                {
                                    buildingTemplateList.Add(BlobStorageTemplateInfo.FromJObject((JObject)entry));
                                }
                                catch (ArgumentException ex)
                                {
                                    environmentSettings.Host.Logger.LogDebug($"Failed to read template info entry, missing mandatory fields. Details: {ex}");
                                }
                            }
                        }
                    }

                    templateList = buildingTemplateList;
                    return true;
                }

                templateList = null;
                return false;
            }
            catch (Exception ex)
            {
                environmentSettings.Host.Logger.LogDebug($"Failed to read template info entries. Details: {ex}");
                templateList = null;
                return false;
            }
        }

        private static bool TryReadPackToTemplateMap(JObject cacheObject, out IReadOnlyDictionary<string, PackToTemplateEntry> packToTemplateMap)
        {
            try
            {
                if (!cacheObject.TryGetValue(nameof(TemplateDiscoveryMetadata.PackToTemplateMap), out JToken packToTemplateMapToken)
                    || !(packToTemplateMapToken is JObject packToTemplateMapObject))
                {
                    packToTemplateMap = null;
                    return false;
                }

                Dictionary<string, PackToTemplateEntry> workingPackToTemplateMap = new Dictionary<string, PackToTemplateEntry>();

                foreach (JProperty packEntry in packToTemplateMapObject.Properties())
                {
                    if (packEntry != null)
                    {
                        string packName = packEntry.Name.ToString();
                        JObject entryValue = (JObject)packEntry.Value;

                        if (entryValue.TryGetValue(nameof(PackToTemplateEntry.Version), StringComparison.OrdinalIgnoreCase, out JToken versionToken)
                            && versionToken.Type == JTokenType.String
                            && entryValue.TryGetValue(nameof(PackToTemplateEntry.TemplateIdentificationEntry), StringComparison.OrdinalIgnoreCase, out JToken identificationToken)
                            && identificationToken is JArray identificationArray)
                        {
                            string version = versionToken.Value<string>();
                            List<TemplateIdentificationEntry> templatesInPack = new List<TemplateIdentificationEntry>();

                            foreach (JObject templateIdentityInfo in identificationArray)
                            {
                                string identity = templateIdentityInfo.Value<string>(nameof(TemplateIdentificationEntry.Identity));
                                string groupIdentity = templateIdentityInfo.Value<string>(nameof(TemplateIdentificationEntry.GroupIdentity));

                                TemplateIdentificationEntry deserializedEntry = new TemplateIdentificationEntry(identity, groupIdentity);
                                templatesInPack.Add(deserializedEntry);
                            }

                            workingPackToTemplateMap[packName] = new PackToTemplateEntry(version, templatesInPack);
                            if (entryValue.TryGetValue(nameof(PackToTemplateEntry.TotalDownloads), out JToken totalDownloadsToken)
                                && long.TryParse(totalDownloadsToken.Value<string>(), out long totalDownloads))
                            {
                                workingPackToTemplateMap[packName].TotalDownloads = totalDownloads;
                            }
                        }
                    }
                }

                packToTemplateMap = workingPackToTemplateMap;
                return true;
            }
            catch
            {
                packToTemplateMap = null;
                return false;
            }
        }

        private static bool TryReadAdditionalData(JObject cacheObject, IReadOnlyDictionary<string, Func<JObject, object>> additionalDataReaders, out IReadOnlyDictionary<string, object> additionalData)
        {
            // get the additional data section
            if (!cacheObject.TryGetValue(nameof(TemplateDiscoveryMetadata.AdditionalData), out JToken additionalDataToken)
                || !(additionalDataToken is JObject additionalDataObject))
            {
                additionalData = null;
                return false;
            }

            Dictionary<string, object> workingAdditionalData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, Func<JObject, object>> dataReadInfo in additionalDataReaders)
            {
                try
                {
                    // get the entry for this piece of additional data
                    if (!additionalDataObject.TryGetValue(dataReadInfo.Key, StringComparison.OrdinalIgnoreCase, out JToken dataToken)
                        || !(dataToken is JObject dataObject))
                    {
                        // this piece of data wasn't found, or wasn't valid. Ignore it.
                        continue;
                    }

                    workingAdditionalData[dataReadInfo.Key] = dataReadInfo.Value(dataObject);
                }
                catch
                {
                    // Do nothing.
                    // This piece of data failed to read, but isn't strictly necessary.
                }
            }

            additionalData = workingAdditionalData;
            return true;
        }
    }
}
