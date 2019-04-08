using System.Collections.Generic;
using Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    internal class TemplatePackSearchResult
    {
        public TemplatePackSearchResult(PackAndVersion packInfo)
        {
            PackInfo = packInfo;
            _templateMatches = new List<ITemplateMatchInfo>();
        }

        public PackAndVersion PackInfo { get; }

        public void AddMatch(ITemplateMatchInfo match)
        {
            _templateMatches.Add(match);
        }

        private List<ITemplateMatchInfo> _templateMatches;

        public IReadOnlyList<ITemplateMatchInfo> TemplateMatches => _templateMatches;
    }
}
