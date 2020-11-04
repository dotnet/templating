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
        public CliHostSpecificDataMatchFilterFactory(INewCommandInput commandInput)
        {
            _commandInput = commandInput;
        }

        private readonly INewCommandInput _commandInput;

        public Func<IReadOnlyList<ITemplateNameSearchResult>, IReadOnlyList<ITemplateMatchInfo>> MatchFilter => (foundPackages) =>
        {
            IEnumerable<Func<INewCommandInput, Func<PackInfo, bool>>> packageFiltersToUse = SupportedFilterOptions.SupportedSearchFilters
                                    .OfType<PackageFilterOption>()
                                    .Select(filter => filter.PackageMatchFilter);

            IEnumerable<ITemplateNameSearchResult> templatesToFilter =
                foundPackages.Where(foundPackage => packageFiltersToUse.All(pf => pf(_commandInput)(foundPackage.PackInfo)));

            return TemplateListResolver.PerformCoreTemplateQueryForSearch(templatesToFilter.Select(x => x.Template), _commandInput).ToList();
        };
    }
}
