using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.TemplateResolution;
using Microsoft.TemplateEngine.Edge.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Cli.CommandParsing
{
    internal class FilterOption
    {
        internal string Name { get; set; }
        internal Func<INewCommandInput, string> FilterValue { get; set; }
        internal Func<INewCommandInput, bool> IsFilterSet { get; set; }
        internal Func<INewCommandInput, Func<ITemplateInfo, MatchInfo?>> TemplateMatchFilter { get; set; }
        internal Func<ListOrHelpTemplateListResolutionResult, bool> MismatchCriteria { get; set; }
    }
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
                return new FilterOption()
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
                return new FilterOption()
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
                return new FilterOption()
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
                return new FilterOption()
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
