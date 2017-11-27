using Microsoft.TemplateEngine.Edge.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    internal class TemplateSourceSearchResult
    {
        public TemplateSourceSearchResult(Guid sourceId, string sourceDisplayName)
        {
            SourceId = sourceId;
            SourceDisplayName = sourceDisplayName;
            _packsWithMatches = new Dictionary<string, TemplatePackSearchResult>();
        }

        public Guid SourceId { get; }

        public string SourceDisplayName { get; }

        public void AddMatchForPack(string packName, ITemplateMatchInfo matchInfo)
        {
            if (!_packsWithMatches.TryGetValue(packName, out TemplatePackSearchResult matchesForPack))
            {
                matchesForPack = new TemplatePackSearchResult(packName);
                _packsWithMatches[packName] = matchesForPack;
            }

            matchesForPack.AddMatch(matchInfo);
        }

        private Dictionary<string, TemplatePackSearchResult> _packsWithMatches;

        public IReadOnlyDictionary<string, TemplatePackSearchResult> PacksWithMatches => _packsWithMatches;
    }
}
