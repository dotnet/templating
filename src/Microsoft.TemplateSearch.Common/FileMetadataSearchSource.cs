// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatePackage;

namespace Microsoft.TemplateSearch.Common
{
    // Always inherit from this, don't make it non-abstract.
    // Making this be not abstract will cause problems with the registered components.
    public abstract class FileMetadataSearchSource : ITemplateSearchSource
    {
        private ISearchPackFilter _packFilter;

        public abstract string DisplayName { get; }

        public abstract Guid Id { get; }

        protected IFileMetadataTemplateSearchCache SearchCache { get; set; }

        public abstract Task<bool> TryConfigureAsync(IEngineEnvironmentSettings environmentSettings, IReadOnlyList<IManagedTemplatePackage> existingTemplatePackages, CancellationToken cancellationToken);

        public Task<IReadOnlyList<ITemplateNameSearchResult>> CheckForTemplateNameMatchesAsync(string searchName, CancellationToken cancellationToken)
        {
            if (SearchCache == null)
            {
                throw new Exception("Search Source is not configured");
            }
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyList<ITemplateInfo> templateMatches = SearchCache.GetNameMatchedTemplates(searchName);
            IReadOnlyList<string> templateIdentities = templateMatches.Select(t => t.Identity).ToList();
            IReadOnlyDictionary<string, PackInfo> templateToPackMap = SearchCache.GetTemplateToPackMapForTemplateIdentities(templateIdentities);

            List<ITemplateNameSearchResult> resultList = new List<ITemplateNameSearchResult>();

            foreach (ITemplateInfo candidateTemplateInfo in templateMatches)
            {
                if (!templateToPackMap.TryGetValue(candidateTemplateInfo.Identity, out PackInfo candidatePackInfo))
                {
                    // The pack was not found for the template. This can't realistically occur.
                    // The continue safeguards against the possibility that somehow the map got messed up.
                    continue;
                }

                if (_packFilter.ShouldPackBeFiltered(candidatePackInfo.Name, candidatePackInfo.Version))
                {
                    // something already installed says this should be filtered.
                    continue;
                }

                ITemplateNameSearchResult result = CreateNameSearchResult(candidateTemplateInfo, candidatePackInfo);

                resultList.Add(result);
            }

            return Task.FromResult((IReadOnlyList<ITemplateNameSearchResult>)resultList);
        }

        public Task<IReadOnlyDictionary<string, PackToTemplateEntry>> CheckForTemplatePackMatchesAsync(IReadOnlyList<string> packNameList, CancellationToken cancellationToken)
        {
            if (SearchCache == null)
            {
                throw new Exception("Search Source is not configured");
            }
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyDictionary<string, PackToTemplateEntry> matchedPacks = SearchCache.GetInfoForNamedPacks(packNameList)
                                    .Where(packInfo => !_packFilter.ShouldPackBeFiltered(packInfo.Key, packInfo.Value.Version))
                                    .ToDictionary(packInfo => packInfo.Key, packInfo => packInfo.Value);

            return Task.FromResult(matchedPacks);
        }

        protected void Configure(IFileMetadataTemplateSearchCache searchCache, ISearchPackFilter packFilter)
        {
            SearchCache = searchCache;
            _packFilter = packFilter;
        }

        protected virtual TemplateNameSearchResult CreateNameSearchResult(ITemplateInfo candidateTemplateInfo, PackInfo candidatePackInfo)
        {
            return new TemplateNameSearchResult(candidateTemplateInfo, candidatePackInfo);
        }
    }
}
