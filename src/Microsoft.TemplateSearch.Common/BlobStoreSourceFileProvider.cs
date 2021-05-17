// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;

namespace Microsoft.TemplateSearch.Common
{
    internal class BlobStoreSourceFileProvider : ISearchInfoFileProvider
    {
        private const int CachedFileValidityInHours = 1;
        private const string ETagFileSuffix = ".etag";
        private const string ETagHeaderName = "ETag";
        private const string IfNoneMatchHeaderName = "If-None-Match";
        private const string _localSourceSearchFileOverrideEnvVar = "DOTNET_NEW_SEARCH_FILE_OVERRIDE";
        private const string _useLocalSearchFileIfPresentEnvVar = "DOTNET_NEW_LOCAL_SEARCH_FILE_ONLY";
        private static readonly Uri _searchMetadataUri = new Uri("https://go.microsoft.com/fwlink/?linkid=2087906&clcid=0x409");

        public BlobStoreSourceFileProvider()
        {
        }

        public async Task<bool> TryEnsureSearchFileAsync(IEngineEnvironmentSettings environmentSettings, string metadataFileTargetLocation)
        {
            string? localOverridePath = environmentSettings.Environment.GetEnvironmentVariable(_localSourceSearchFileOverrideEnvVar);
            if (!string.IsNullOrEmpty(localOverridePath))
            {
                if (environmentSettings.Host.FileSystem.FileExists(localOverridePath!))
                {
                    environmentSettings.Host.FileSystem.FileCopy(localOverridePath!, metadataFileTargetLocation, true);
                    return true;
                }

                return false;
            }

            string? useLocalSearchFile = environmentSettings.Environment.GetEnvironmentVariable(_useLocalSearchFileIfPresentEnvVar);
            if (!string.IsNullOrEmpty(useLocalSearchFile))
            {
                // evn var is set, only use a local copy of the search file. Don't try to acquire one from blob storage.
                return TryUseLocalSearchFile(environmentSettings, metadataFileTargetLocation);
            }
            else
            {
                // prefer a search file from cloud storage.
                // only download the file if it's been long enough since the last time it was downloaded.
                if (ShouldDownloadFileFromCloud(environmentSettings, metadataFileTargetLocation))
                {
                    bool cloudResult = await TryAcquireFileFromCloudAsync(environmentSettings, metadataFileTargetLocation).ConfigureAwait(false);

                    if (cloudResult)
                    {
                        return true;
                    }
                }
                else
                {
                    // the file exists and is new enough to not download again.
                    return true;
                }

                // no cloud store file was available. Use a local file if possible.
                return TryUseLocalSearchFile(environmentSettings, metadataFileTargetLocation);
            }
        }

        private bool ShouldDownloadFileFromCloud(IEngineEnvironmentSettings environmentSettings, string metadataFileTargetLocation)
        {
            if (environmentSettings.Host.FileSystem.FileExists(metadataFileTargetLocation))
            {
                DateTime utcNow = DateTime.UtcNow;
                DateTime lastWriteTimeUtc = environmentSettings.Host.FileSystem.GetLastWriteTimeUtc(metadataFileTargetLocation);
                if (lastWriteTimeUtc.AddHours(CachedFileValidityInHours) > utcNow)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryUseLocalSearchFile(IEngineEnvironmentSettings environmentSettings, string metadataFileTargetLocation)
        {
            // A previously acquired file may already be setup.
            // It could either be from online storage, or shipped in-box.
            // If so, fallback to using it.
            if (environmentSettings.Host.FileSystem.FileExists(metadataFileTargetLocation))
            {
                return true;
            }
            return false;
        }

        // Attempt to get the search metadata file from cloud storage and place it in the expected search location.
        // Return true on success, false on failure.
        // Implement If-None-Match/ETag headers to avoid re-downloading the same content over and over again.
        private async Task<bool> TryAcquireFileFromCloudAsync(IEngineEnvironmentSettings environmentSettings, string searchMetadataFileLocation)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string etagFileLocation = searchMetadataFileLocation + ETagFileSuffix;
                    if (environmentSettings.Host.FileSystem.FileExists(etagFileLocation))
                    {
                        string etagValue = environmentSettings.Host.FileSystem.ReadAllText(etagFileLocation);
                        client.DefaultRequestHeaders.Add(IfNoneMatchHeaderName, $"\"{etagValue}\"");
                    }
                    using (HttpResponseMessage response = client.GetAsync(_searchMetadataUri).Result)
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string resultText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            environmentSettings.Host.FileSystem.WriteAllText(searchMetadataFileLocation, resultText);

                            IEnumerable<string> etagValues;
                            if (response.Headers.TryGetValues(ETagHeaderName, out etagValues))
                            {
                                if (etagValues.Count() == 1)
                                {
                                    environmentSettings.Host.FileSystem.WriteAllText(etagFileLocation, etagValues.First());
                                }
                            }

                            return true;
                        }
                        else if (response.StatusCode == HttpStatusCode.NotModified)
                        {
                            IPhysicalFileSystem fileSystem = environmentSettings.Host.FileSystem;

                            environmentSettings.Host.FileSystem.SetLastWriteTimeUtc(searchMetadataFileLocation, DateTime.UtcNow);
                            return true;
                        }

                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
