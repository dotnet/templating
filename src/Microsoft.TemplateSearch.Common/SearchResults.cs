using System.Collections.Generic;

namespace Microsoft.TemplateSearch.Common
{
    public class SearchResults
    {
        public SearchResults()
        {
            MatchesBySource = new List<TemplatePackageSearchResult>();
            AnySources = false;
        }

        public SearchResults(IReadOnlyList<TemplatePackageSearchResult> matchesBySource, bool anySources)
        {
            MatchesBySource = matchesBySource;
            AnySources = anySources;
        }

        public IReadOnlyList<TemplatePackageSearchResult> MatchesBySource { get; }

        public bool AnySources { get; }
    }
}
