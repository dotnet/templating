using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Cli.TemplateResolution;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateSearch.Common;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    public class CliHostSpecificDataMatchFilterFactory
    {
        public CliHostSpecificDataMatchFilterFactory(INewCommandInput commandInput, string defaultLanguage)
        {
            _commandInput = commandInput;
            _defaultLanguage = defaultLanguage;
        }

        private readonly INewCommandInput _commandInput;
        private readonly string _defaultLanguage;

        public Func<IReadOnlyList<ITemplateNameSearchResult>, IReadOnlyList<ITemplateMatchInfo>> MatchFilter => (nameMatches) =>
        {
            IEnumerable<ITemplateNameSearchResult> templatesToFilter = nameMatches;
            if (!string.IsNullOrEmpty(_commandInput.PackageFilter))
            {
                templatesToFilter = nameMatches.Where(pack => pack.PackInfo.Name.IndexOf(_commandInput.PackageFilter, StringComparison.OrdinalIgnoreCase) > -1);
            }
            return TemplateListResolver.PerformCoreTemplateQueryForSearch(templatesToFilter.Select(x => x.Template), _commandInput).ToList();
        };
    }
}
