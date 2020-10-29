
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Utils;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateSearch.Common;
using Microsoft.TemplateEngine.Cli.TableOutput;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    internal static class CliTemplateSearch
    {

        private static readonly Dictionary<string, Func<INewCommandInput, string>> SupportedFilterList = new Dictionary<string, Func<INewCommandInput, string>>()
        {
                {"--author", command => command.AuthorFilter },
                {"--type", command => command.TypeFilter },
                {"--language", command => command.Language },
                {"--package", command => command.PackageFilter },
                {"--baseline", command => command.BaselineName }
        };

        private static string SupportedFilterOptions
        {
            get
            {
                return string.Join(", ", SupportedFilterList.Keys.Select(s => $"'{s}'"));
            }
        }

        internal static async Task<CreationResultStatus> SearchForTemplateMatchesAsync(IEngineEnvironmentSettings environmentSettings, INewCommandInput commandInput, string defaultLanguage)
        {

            if (!ValidateCommandInput(commandInput))
            {
                return CreationResultStatus.Cancelled;
            }

            Reporter.Output.WriteLine(LocalizableStrings.SearchOnlineNotification);
            TemplateSearchCoordinator searchCoordinator = CliTemplateSearchCoordinatorFactory.CreateCliTemplateSearchCoordinator(environmentSettings, commandInput, defaultLanguage);
            SearchResults searchResults = await searchCoordinator.SearchAsync();

            if (!searchResults.AnySources)
            {
                Reporter.Error.WriteLine(LocalizableStrings.SearchOnlineNoSources.Bold().Red());
                return CreationResultStatus.NotFound;
            }

            if (searchResults.MatchesBySource.Count > 0)
            {
                string packageIdToShow = null;
                foreach (TemplateSourceSearchResult sourceResult in searchResults.MatchesBySource)
                {
                    DisplayResultsForPack(sourceResult, environmentSettings, commandInput, defaultLanguage);

                    var firstMicrosoftAuthoredPack = sourceResult.PacksWithMatches.FirstOrDefault(p => p.Value.TemplateMatches.Any(t => string.Equals(t.Info.Author, "Microsoft")));
                    if (!firstMicrosoftAuthoredPack.Equals(default(KeyValuePair<PackInfo, TemplatePackSearchResult>)))
                    {
                        packageIdToShow = firstMicrosoftAuthoredPack.Key.Name;
                    }
                }

                Reporter.Output.WriteLine();
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.SearchResultInstallHeader, commandInput.CommandName));
                if (string.IsNullOrWhiteSpace(packageIdToShow))
                {
                    packageIdToShow = searchResults.MatchesBySource.First().PacksWithMatches.First().Key.Name;
                }
                Reporter.Output.WriteLine("Example:");
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.SearchResultInstallCommand, commandInput.CommandName, packageIdToShow));
                return CreationResultStatus.Success;
            }
            else
            {
                string filters = string.Join(", ", SupportedFilterList.Where(filter => !string.IsNullOrWhiteSpace(filter.Value(commandInput))).Select(filter => $"{filter.Key}='{filter.Value(commandInput)}'"));
                string searchCriteria = string.IsNullOrWhiteSpace(commandInput.TemplateName)
                    ? filters
                    : string.IsNullOrWhiteSpace(filters) ? commandInput.TemplateName : string.Join(", ", commandInput.TemplateName, filters);

                Reporter.Error.WriteLine(string.Format(LocalizableStrings.NoTemplatesMatchingInputParameters, searchCriteria).Bold().Red());
                return CreationResultStatus.NotFound;
            }
        }

        private static bool ValidateCommandInput(INewCommandInput commandInput)
        {
            if (string.IsNullOrWhiteSpace(commandInput.TemplateName) && SupportedFilterList.Values.All(filter => string.IsNullOrWhiteSpace(filter(commandInput))))
            {
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.SearchOnlineErrorNoTemplateNameOrFilter, SupportedFilterOptions, commandInput.CommandName).Bold().Red());
                return false;
            }

            if (!string.IsNullOrWhiteSpace(commandInput.TemplateName) && commandInput.TemplateName.Length < 2)
            {
                Reporter.Error.WriteLine(LocalizableStrings.SearchOnlineErrorTemplateNameIsTooShort.Bold().Red());
                return false;
            }

            return true;
        }

        private class SearchResultTableRow
        {
            internal SearchResultTableRow (TemplateGroupTableRow templateGroupTableRow, string packageName)
            {
                TemplateGroupInfo = templateGroupTableRow;
                PackageName = packageName;
            }
            internal string PackageName { get; set; }
            internal TemplateGroupTableRow TemplateGroupInfo { get; set; }
        }

        private static IReadOnlyCollection<SearchResultTableRow> GetSearchResultsForDisplay(TemplateSourceSearchResult sourceResult, string language, string defaultLanguage)
        {
            List<SearchResultTableRow> templateGroupsForDisplay = new List<SearchResultTableRow>();

            foreach (TemplatePackSearchResult packSearchResult in sourceResult.PacksWithMatches.Values)
            {
                var templateGroupsForPack = TemplateGroupDisplay.GetTemplateGroupsForListDisplay(packSearchResult.TemplateMatches, language, defaultLanguage);
                templateGroupsForDisplay.AddRange(templateGroupsForPack.Select(t => new SearchResultTableRow(t, packSearchResult.PackInfo.Name))); 
            }

            return templateGroupsForDisplay;
        }

        private static void DisplayResultsForPack(TemplateSourceSearchResult sourceResult, IEngineEnvironmentSettings environmentSettings, INewCommandInput commandInput, string defaultLanguage)
        {
            string sourceHeader = string.Format(LocalizableStrings.SearchResultSourceIndicator, sourceResult.SourceDisplayName);

            Reporter.Output.WriteLine(sourceHeader);
            Reporter.Output.WriteLine();

            IReadOnlyCollection<SearchResultTableRow> data = GetSearchResultsForDisplay(sourceResult, commandInput.Language, defaultLanguage);

            HelpFormatter<SearchResultTableRow> formatter =
                HelpFormatter
                    .For(
                        environmentSettings,
                        commandInput,
                        data,
                        columnPadding: 2,
                        headerSeparator: '-',
                        blankLineBetweenRows: false)
                    .DefineColumn(r => r.PackageName, out object packageColumn, LocalizableStrings.ColumnNamePackage, showAlways: true)
                    .DefineColumn(r => r.TemplateGroupInfo.Name, LocalizableStrings.ColumnNameTemplateName, showAlways: true, shrinkIfNeeded: true, minWidth:15)
                    .DefineColumn(r => r.TemplateGroupInfo.ShortName, LocalizableStrings.ColumnNameShortName, showAlways: true)
                    .DefineColumn(r => r.TemplateGroupInfo.Languages, LocalizableStrings.ColumnNameLanguage, NewCommandInputCli.LanguageColumnFilter, defaultColumn: true)
                    .DefineColumn(r => r.TemplateGroupInfo.Classifications, LocalizableStrings.ColumnNameTags, NewCommandInputCli.TagsColumnFilter, defaultColumn: true)
                    .DefineColumn(r => r.TemplateGroupInfo.Author, LocalizableStrings.ColumnNameAuthor, NewCommandInputCli.AuthorColumnFilter, defaultColumn: false, shrinkIfNeeded: true, minWidth: 10)
                    .DefineColumn(r => r.TemplateGroupInfo.Type, LocalizableStrings.ColumnNameType, NewCommandInputCli.TypeColumnFilter, defaultColumn: false)
                    .OrderBy(packageColumn);

            Reporter.Output.WriteLine(formatter.Layout());
        }
    }
}
