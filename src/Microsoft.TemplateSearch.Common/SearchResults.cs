using System.Collections.Generic;

namespace Microsoft.TemplateSearch.Common
{
    public class SearchResults
    {
        public SearchResults()
        {
            MatchesBySource = new List<TemplateSourceSearchResult>();
            AnySources = false;
        }

        public SearchResults(IReadOnlyList<TemplateSourceSearchResult> matchesBySource, bool anySources)
        {
            MatchesBySource = matchesBySource;
            AnySources = anySources;
        }

        public IReadOnlyList<TemplateSourceSearchResult> MatchesBySource { get; }

        public bool AnySources { get; }
    }
}
