// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateSearch.Common;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    internal class CliNuGetMetadataTemplateSearchCache : FileMetadataTemplateSearchCache
    {
        internal CliNuGetMetadataTemplateSearchCache(IEngineEnvironmentSettings environmentSettings, string pathToMetadata)
            : base(environmentSettings, pathToMetadata)
        {
        }

        protected IReadOnlyDictionary<string, HostSpecificTemplateData> CliHostSpecificData { get; set; }

        internal IReadOnlyDictionary<string, HostSpecificTemplateData> GetHostDataForTemplateIdentities(IReadOnlyList<string> identities)
        {
            EnsureInitialized();

            Dictionary<string, HostSpecificTemplateData> map = new Dictionary<string, HostSpecificTemplateData>();

            foreach (string templateIdentity in identities)
            {
                if (CliHostSpecificData.TryGetValue(templateIdentity, out HostSpecificTemplateData hostData))
                {
                    map[templateIdentity] = hostData;
                }
            }

            return map;
        }

        internal bool TryGetHostDataForTemplateIdentity(string identity, out HostSpecificTemplateData hostData)
        {
            EnsureInitialized();

            return CliHostSpecificData.TryGetValue(identity, out hostData);
        }

        protected override NuGetSearchCacheConfig SetupSearchCacheConfig()
        {
            return new CliNuGetSearchCacheConfig(PathToMetadta);
        }

        protected override void EnsureInitialized()
        {
            if (IsInitialized)
            {
                return;
            }

            base.EnsureInitialized();

            SetupCliHostSpecificData();
        }

        protected void SetupCliHostSpecificData()
        {
            try
            {
                if (TemplateDiscoveryMetadata.AdditionalData.TryGetValue(CliNuGetSearchCacheConfig.CliHostDataName, out object cliHostDataObject))
                {
                    CliHostSpecificData = (Dictionary<string, HostSpecificTemplateData>)cliHostDataObject;
                    return;
                }
            }
            catch
            {
                // It's ok for the host data to not exist, or not be properly read.
            }

            // set a default for when there isn't any in the discovery metadata, or when there's an exception.
            CliHostSpecificData = new Dictionary<string, HostSpecificTemplateData>();
        }
    }
}
