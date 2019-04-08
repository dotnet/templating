using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateSearch.Common;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource
{
    public class FileMetadataTemplateSearchCache
    {
        private readonly IEngineEnvironmentSettings _environment;
        private readonly ISearchCacheConfig _config;
        private bool _isInitialized;
        private TemplateDiscoveryMetadata _templateDiscoveryMetadata;
        private TemplateToPackMap _templateToPackMap;
        private IReadOnlyDictionary<string, HostSpecificTemplateData> _cliHostSpecificData;

        public FileMetadataTemplateSearchCache(IEngineEnvironmentSettings environmentSettings, string pathToMetadata)
        {
            _environment = environmentSettings;
            _config = new NuGetSearchCacheConfig(pathToMetadata);
            _isInitialized = false;
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            if (FileMetadataTemplateSearchCacheReader.TryReadDiscoveryMetadata(_environment, _config, out _templateDiscoveryMetadata))
            {
                _templateToPackMap = TemplateToPackMap.FromPackToTemplateDictionary(_templateDiscoveryMetadata.PackToTemplateMap);

                try
                {
                    if (_templateDiscoveryMetadata.AdditionalData.TryGetValue(NuGetSearchCacheConfig._cliHostDataName, out object cliHostDataObject))
                    {
                        _cliHostSpecificData = (Dictionary<string, HostSpecificTemplateData>)cliHostDataObject;
                    }
                }
                catch
                {
                    // It's ok for the host data to not exist, or not be properly read.
                    _cliHostSpecificData = new Dictionary<string, HostSpecificTemplateData>();
                }

                _isInitialized = true;
            }
            else
            {
                throw new Exception("Error reading template search metadata");
            }
        }

        public IReadOnlyList<ITemplateInfo> GetNameMatchedTemplates(string searchName)
        {
            EnsureInitialized();

            return _templateDiscoveryMetadata.TemplateCache.Where(template => template.Name.IndexOf(searchName, StringComparison.OrdinalIgnoreCase) >= 0
                                                    || template.ShortName.IndexOf(searchName, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        public IReadOnlyDictionary<string, PackAndVersion> GetTemplateToPackMapForTemplateIdentities(IReadOnlyList<string> identities)
        {
            EnsureInitialized();

            Dictionary<string, PackAndVersion> map = new Dictionary<string, PackAndVersion>();

            foreach (string templateIdentity in identities)
            {
                if (_templateToPackMap.TryGetPackInfoForTemplateIdentity(templateIdentity, out PackAndVersion packInfo))
                {
                    map[templateIdentity] = packInfo;
                }
            }

            return map;
        }

        public IReadOnlyDictionary<string, HostSpecificTemplateData> GetHostDataForTemplateIdentities(IReadOnlyList<string> identities)
        {
            EnsureInitialized();

            Dictionary<string, HostSpecificTemplateData> map = new Dictionary<string, HostSpecificTemplateData>();

            foreach (string templateIdentity in identities)
            {
                if (_cliHostSpecificData.TryGetValue(templateIdentity, out HostSpecificTemplateData hostData))
                {
                    map[templateIdentity] = hostData;
                }
            }

            return map;
        }
    }
}
