using System.Collections.Generic;
using Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    internal class TemplateSourceSearchResult
    {
        public TemplateSourceSearchResult(string sourceDisplayName)
        {
            SourceDisplayName = sourceDisplayName;
            _packsWithMatches = new Dictionary<PackAndVersion, TemplatePackSearchResult>();
        }

        public string SourceDisplayName { get; }

        public void AddMatchForPack(PackAndVersion packInfo, ITemplateMatchInfo matchInfo)
        {
            if (!_packsWithMatches.TryGetValue(packInfo, out TemplatePackSearchResult matchesForPack))
            {
                matchesForPack = new TemplatePackSearchResult(packInfo);
                _packsWithMatches[packInfo] = matchesForPack;
            }

            matchesForPack.AddMatch(matchInfo);
        }

        private Dictionary<PackAndVersion, TemplatePackSearchResult> _packsWithMatches;

        public IReadOnlyDictionary<PackAndVersion, TemplatePackSearchResult> PacksWithMatches => _packsWithMatches;
    }
}
