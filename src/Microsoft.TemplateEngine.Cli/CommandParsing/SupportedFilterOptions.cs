using Microsoft.TemplateEngine.Cli.TemplateResolution;
using Microsoft.TemplateEngine.Edge.Template;
using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Cli.CommandParsing
{
    internal static class SupportedFilterOptions
    {

        internal static readonly IReadOnlyCollection<FilterOption> SupportedListFilters = new List<FilterOption>()
        {
            SupportedFilterOptions.AuthorFilter,
            SupportedFilterOptions.BaselineFilter,
            SupportedFilterOptions.LanguageFilter,
            SupportedFilterOptions.TypeFilter
        };

        internal static FilterOption BaselineFilter
        {
            get
            {
                return new TemplateFilterOption()
                {
                    Name = "baseline",
                    FilterValue = command => command.BaselineName,
                    IsFilterSet = command => !string.IsNullOrWhiteSpace(command.BaselineName),
                    TemplateMatchFilter = command => WellKnownSearchFilters.BaselineFilter(command.BaselineName),
                    MismatchCriteria = resolutionResult => resolutionResult.HasBaselineMismatch
                };
            }
        }

        internal static FilterOption AuthorFilter
        {
            get
            {
                return new TemplateFilterOption()
                {
                    Name = "author",
                    FilterValue = command => command.AuthorFilter,
                    IsFilterSet = command => !string.IsNullOrWhiteSpace(command.AuthorFilter),
                    TemplateMatchFilter = command => WellKnownSearchFilters.AuthorFilter(command.AuthorFilter),
                    MismatchCriteria = resolutionResult => resolutionResult.HasAuthorMismatch
                };
            }
        }

        internal static FilterOption LanguageFilter
        {
            get
            {
                return new TemplateFilterOption()
                {
                    Name = "language",
                    FilterValue = command => command.Language,
                    IsFilterSet = command => !string.IsNullOrWhiteSpace(command.Language),
                    TemplateMatchFilter = command => WellKnownSearchFilters.LanguageFilter(command.Language),
                    MismatchCriteria = resolutionResult => resolutionResult.HasLanguageMismatch
                };
            }
        }

        internal static FilterOption TypeFilter
        {
            get
            {
                return new TemplateFilterOption ()
                {
                    Name = "type",
                    FilterValue = command => command.TypeFilter,
                    IsFilterSet = command => !string.IsNullOrWhiteSpace(command.TypeFilter),
                    TemplateMatchFilter = command => WellKnownSearchFilters.ContextFilter(command.TypeFilter),
                    MismatchCriteria = resolutionResult => resolutionResult.HasContextMismatch
                };
            }
        }



    }
}
