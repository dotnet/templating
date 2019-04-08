using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource
{
    public abstract class FileMetadataSearchSource : ITemplateSearchSource
    {
        private FileMetadataTemplateSearchCache _searchCache;

        public abstract bool TryConfigure(IEngineEnvironmentSettings environmentSettings);

        protected void Configure(FileMetadataTemplateSearchCache searchCache)
        {
            _searchCache = searchCache;
        }

        public abstract string DisplayName { get; }

        public Guid Id => new Guid("6EA368C4-8A56-444C-91D1-55150B296BF2");

        public Task<IReadOnlyList<ITemplateNameSearchResult>> CheckForTemplateNameMatchesAsync(string searchName)
        {
            if (_searchCache == null)
            {
                throw new Exception("Search Source is not configured");
            }

            IReadOnlyList<ITemplateInfo> templateMatches = _searchCache.GetNameMatchedTemplates(searchName);
            IReadOnlyList<string> templateIdentities = templateMatches.Select(t => t.Identity).ToList();
            IReadOnlyDictionary<string, PackAndVersion> templateToPackMap = _searchCache.GetTemplateToPackMapForTemplateIdentities(templateIdentities);
            IReadOnlyDictionary<string, HostSpecificTemplateData> hostDataLookup = _searchCache.GetHostDataForTemplateIdentities(templateIdentities);

            List<ITemplateNameSearchResult> resultList = new List<ITemplateNameSearchResult>();

            foreach (ITemplateInfo templateInfo in templateMatches)
            {
                if (!templateToPackMap.TryGetValue(templateInfo.Identity, out PackAndVersion packInfo))
                {
                    // The pack was not found for the template. This can't realistically occur.
                    // The continue safeguards against the possibility that somehow the map got messed up.
                    continue;
                }

                TemplateNameSearchResult result;

                if (hostDataLookup.TryGetValue(templateInfo.Identity, out HostSpecificTemplateData hostData))
                {
                    result = new TemplateNameSearchResult(templateInfo, hostData, packInfo);
                }
                else
                {
                    result = new TemplateNameSearchResult(templateInfo, packInfo);
                }

                resultList.Add(result);
            }

            return Task.FromResult((IReadOnlyList<ITemplateNameSearchResult>)resultList);
        }
    }
}
