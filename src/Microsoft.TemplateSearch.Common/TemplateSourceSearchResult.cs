using System.Collections.Generic;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateSearch.Common
{
    public class TemplateSourceSearchResult
    {
        public TemplateSourceSearchResult(string sourceDisplayName)
        {
            SourceDisplayName = sourceDisplayName;
            _packsWithMatches = new Dictionary<PackInfo, TemplatePackSearchResult>(new PackInfoEqualityComparer());
        }

        public string SourceDisplayName { get; }

        public void AddMatchForPack(PackInfo packInfo, ITemplateMatchInfo matchInfo)
        {
            if (!_packsWithMatches.TryGetValue(packInfo, out TemplatePackSearchResult matchesForPack))
            {
                matchesForPack = new TemplatePackSearchResult(packInfo);
                _packsWithMatches[packInfo] = matchesForPack;
            }

            matchesForPack.AddMatch(matchInfo);
        }

        private Dictionary<PackInfo, TemplatePackSearchResult> _packsWithMatches;

        public IReadOnlyDictionary<PackInfo, TemplatePackSearchResult> PacksWithMatches => _packsWithMatches;
    }
}
