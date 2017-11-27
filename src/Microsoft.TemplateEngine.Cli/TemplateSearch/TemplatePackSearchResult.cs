using System.Collections.Generic;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    internal class TemplatePackSearchResult
    {
        public TemplatePackSearchResult(string packName)
        {
            PackName = packName;
            _templateMatches = new List<ITemplateMatchInfo>();
        }

        public string PackName { get; }

        public void AddMatch(ITemplateMatchInfo match)
        {
            _templateMatches.Add(match);
        }

        private List<ITemplateMatchInfo> _templateMatches;

        public IReadOnlyList<ITemplateMatchInfo> TemplateMatches => _templateMatches;
    }
}
