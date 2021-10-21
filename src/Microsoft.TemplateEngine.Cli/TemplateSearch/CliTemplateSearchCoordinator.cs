// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatePackage;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Cli.Commands;
using Microsoft.TemplateEngine.Cli.TabularOutput;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateSearch.Common;
using Microsoft.TemplateSearch.Common.Abstractions;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    internal static class CliTemplateSearchCoordinator
    {
        /// <summary>
        /// Executes searching for the templates in configured remote sources.
        /// Performs validation for the commands, search for the templates in configured remote source, displays the results in table format.
        /// </summary>
        /// <param name="environmentSettings">environment settings.</param>
        /// <param name="templatePackageManager"></param>
        /// <param name="commandArgs">new command data.</param>
        /// <param name="defaultLanguage">default language for the host.</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="NewCommandStatus.Success"/> when the templates were found and displayed;
        /// <see cref="NewCommandStatus.Cancelled"/> when the command validation fails;
        /// <see cref="NewCommandStatus.NotFound"/> when no templates found based on the filter criteria.
        /// </returns>
        internal static async Task<NewCommandStatus> SearchForTemplateMatchesAsync(
            IEngineEnvironmentSettings environmentSettings,
            TemplatePackageManager templatePackageManager,
            SearchCommandArgs commandArgs,
            string? defaultLanguage,
            CancellationToken cancellationToken)
        {
            if (!ValidateCommandInput(commandArgs))
            {
                return NewCommandStatus.InvalidParamValues;
            }

            Reporter.Output.WriteLine(LocalizableStrings.CliTemplateSearchCoordinator_Info_SearchInProgress);
            IReadOnlyList<IManagedTemplatePackage> templatePackages =
                await templatePackageManager.GetManagedTemplatePackagesAsync(force: false, cancellationToken: cancellationToken).ConfigureAwait(false);

            TemplateSearchCoordinator searchCoordinator = CliTemplateSearchCoordinatorFactory.CreateCliTemplateSearchCoordinator(environmentSettings);
            CliSearchFiltersFactory searchFiltersFactory = new CliSearchFiltersFactory(templatePackages);

            IReadOnlyList<SearchResult>? searchResults = await searchCoordinator.SearchAsync(
                searchFiltersFactory.GetPackFilter(commandArgs),
                CliSearchFiltersFactory.GetMatchingTemplatesFilter(commandArgs),
                cancellationToken).ConfigureAwait(false);

            if (!searchResults.Any())
            {
                Reporter.Error.WriteLine(LocalizableStrings.CliTemplateSearchCoordinator_Error_NoSources.Bold().Red());
                return NewCommandStatus.NotFound;
            }

            foreach (SearchResult result in searchResults)
            {
                if (!result.Success)
                {
                    Reporter.Error.WriteLine(string.Format(LocalizableStrings.CliTemplateSearchCoordinator_Info_MatchesFromSource, result.Provider.Factory.DisplayName));
                    Reporter.Error.WriteLine(string.Format(LocalizableStrings.CliTemplateSearchCoordinator_Error_SearchFailure, result.ErrorMessage).Red().Bold());
                    continue;
                }

                Reporter.Output.WriteLine(string.Format(LocalizableStrings.CliTemplateSearchCoordinator_Info_MatchesFromSource, result.Provider.Factory.DisplayName));
                if (result.SearchHits.Any())
                {
                    DisplayResultsForPack(result.SearchHits, environmentSettings, commandArgs, defaultLanguage);
                }
                else
                {
                    //TODO: implement it for template options matching
                    //IReadOnlyDictionary<string, string?>? appliedParameterMatches = TemplateCommandInput.GetTemplateParametersFromCommand(commandArgs);
                    // No templates found matching the following input parameter(s): {0}.
                    Reporter.Error.WriteLine(
                        string.Format(
                            LocalizableStrings.NoTemplatesMatchingInputParameters,
                            GetInputParametersString(commandArgs))
                        .Bold().Red());
                }
            }
            Reporter.Output.WriteLine();
            if (searchResults.Where(r => r.Success).SelectMany(r => r.SearchHits).Any())
            {
                string packageIdToShow = EvaluatePackageToShow(searchResults);
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.CliTemplateSearchCoordinator_Info_InstallHelp, commandArgs.CommandName));
                Reporter.Output.WriteCommand(CommandExamples.InstallCommandExample(commandArgs.CommandName));
                Reporter.Output.WriteLine(LocalizableStrings.Generic_ExampleHeader);
                Reporter.Output.WriteCommand(CommandExamples.InstallCommandExample(commandArgs.CommandName, packageID: packageIdToShow));
                return NewCommandStatus.Success;
            }
            return NewCommandStatus.NotFound;
        }

        private static string EvaluatePackageToShow(IReadOnlyList<SearchResult> searchResults)
        {
            var microsoftAuthoredPackages = searchResults
                .SelectMany(r => r.SearchHits)
                .Where(hit => hit.PackageInfo.Name.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase)
                                && hit.MatchedTemplates.Any(t => t.Author == "Microsoft"))
                .OrderByDescending(hit => hit.PackageInfo.TotalDownloads);

            if (microsoftAuthoredPackages.Any())
            {
                return microsoftAuthoredPackages.First().PackageInfo.Name;
            }
            else
            {
                return searchResults
                        .SelectMany(r => r.SearchHits)
                        .OrderByDescending(hit => hit.PackageInfo.TotalDownloads)
                        .First().PackageInfo.Name;
            }
        }

        private static void DisplayResultsForPack(
            IReadOnlyList<(ITemplatePackageInfo PackageInfo, IReadOnlyList<ITemplateInfo> MatchedTemplates)> results,
            IEngineEnvironmentSettings environmentSettings,
            SearchCommandArgs commandArgs,
            string? defaultLanguage)
        {
            //TODO: implement it for template options matching
            //IReadOnlyDictionary<string, string?>? appliedParameterMatches = TemplateCommandInput.GetTemplateParametersFromCommand(commandArgs);
            Reporter.Output.WriteLine(
                string.Format(
                    LocalizableStrings.TemplatesFoundMatchingInputParameters,
                    GetInputParametersString(commandArgs)));
            Reporter.Output.WriteLine();
            IReadOnlyCollection<SearchResultTableRow> data = GetSearchResultsForDisplay(results, commandArgs.Language, defaultLanguage, environmentSettings.Environment);

            TabularOutput<SearchResultTableRow> formatter =
                TabularOutput.TabularOutput
                    .For(
                        new CliTabularOutputSettings(environmentSettings.Environment, commandArgs),
                        data)
                    .DefineColumn(r => r.TemplateGroupInfo.Name, out object nameColumn, LocalizableStrings.ColumnNameTemplateName, showAlways: true, shrinkIfNeeded: true, minWidth: 15)
                    .DefineColumn(r => r.TemplateGroupInfo.ShortNames, LocalizableStrings.ColumnNameShortName, showAlways: true)
                    .DefineColumn(r => r.TemplateGroupInfo.Author, LocalizableStrings.ColumnNameAuthor, BaseCommandInput.AuthorColumnFilter, defaultColumn: true, shrinkIfNeeded: true, minWidth: 10)
                    .DefineColumn(r => r.TemplateGroupInfo.Languages, LocalizableStrings.ColumnNameLanguage, BaseCommandInput.LanguageColumnFilter, defaultColumn: true)
                    .DefineColumn(r => r.TemplateGroupInfo.Type, LocalizableStrings.ColumnNameType, BaseCommandInput.TypeColumnFilter, defaultColumn: false)
                    .DefineColumn(r => r.TemplateGroupInfo.Classifications, LocalizableStrings.ColumnNameTags, BaseCommandInput.TagsColumnFilter, defaultColumn: false, shrinkIfNeeded: true, minWidth: 10)
                    .DefineColumn(r => r.PackageName, out object packageColumn, LocalizableStrings.ColumnNamePackage, showAlways: true)
                    .DefineColumn(r => r.PrintableTotalDownloads, LocalizableStrings.ColumnNameTotalDownloads, showAlways: true, rightAlign: true)
                    .OrderBy(nameColumn, StringComparer.OrdinalIgnoreCase);

            Reporter.Output.WriteLine(formatter.Layout());
        }

        private static IReadOnlyCollection<SearchResultTableRow> GetSearchResultsForDisplay(
            IReadOnlyList<(ITemplatePackageInfo PackageInfo, IReadOnlyList<ITemplateInfo> MatchedTemplates)> results,
            string? language,
            string? defaultLanguage,
            IEnvironment environment)
        {
            List<SearchResultTableRow> templateGroupsForDisplay = new List<SearchResultTableRow>();

            foreach (var packSearchResult in results)
            {
                var templateGroupsForPack = TemplateGroupDisplay.GetTemplateGroupsForListDisplay(packSearchResult.MatchedTemplates, language, defaultLanguage, environment);
                templateGroupsForDisplay.AddRange(templateGroupsForPack.Select(t => new SearchResultTableRow(t, packSearchResult.PackageInfo.Name, packSearchResult.PackageInfo.TotalDownloads)));
            }

            return templateGroupsForDisplay;
        }

        private static bool ValidateCommandInput(SearchCommandArgs commandArgs)
        {
            if (string.IsNullOrWhiteSpace(commandArgs.SearchNameCriteria) && !commandArgs.Filters.Any())
            //TODO: implement it for template options matching
            // && !commandInput.RemainingParameters.Any())
            {
                Reporter.Error.WriteLine(LocalizableStrings.CliTemplateSearchCoordinator_Error_NoTemplateName.Red().Bold());
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.CliTemplateSearchCoordinator_Info_SearchHelp, string.Join(", ", SearchCommand.SupportedFilters.Select(f => $"'{f.Name}'"))));
                Reporter.Error.WriteLine(LocalizableStrings.Generic_ExamplesHeader);
                Reporter.Error.WriteCommand(CommandExamples.SearchCommandExample(commandArgs.CommandName, usePlaceholder: true));
                Reporter.Error.WriteCommand(CommandExamples.SearchCommandExample(commandArgs.CommandName, additionalArgs: new[] { "--author", "Microsoft" }));
                Reporter.Error.WriteCommand(CommandExamples.SearchCommandExample(commandArgs.CommandName, usePlaceholder: true, additionalArgs: new[] { "--author", "Microsoft" }));
                return false;
            }

            if (!string.IsNullOrWhiteSpace(commandArgs.SearchNameCriteria) && commandArgs.SearchNameCriteria.Length < 2)
            {
                Reporter.Error.WriteLine(LocalizableStrings.CliTemplateSearchCoordinator_Error_TemplateNameIsTooShort.Bold().Red());
                return false;
            }

            return true;
        }

        private static string GetInputParametersString(SearchCommandArgs commandArgs/*, IReadOnlyDictionary<string, string?>? templateParameters = null*/)
        {
            string separator = ", ";
            IEnumerable<string> appliedFilters = commandArgs
                .Filters
                .Select(filter => $"{filter.Key.Name}='{filter.Value}'");

            //TODO: implement it for template options matching
            //IEnumerable<string> appliedTemplateParameters = templateParameters?
            //       .Select(param => string.IsNullOrWhiteSpace(param.Value) ? param.Key : $"{param.Key}='{param.Value}'") ?? Array.Empty<string>();

            StringBuilder inputParameters = new StringBuilder();
            string? mainCriteria = commandArgs.SearchNameCriteria;
            if (!string.IsNullOrWhiteSpace(mainCriteria))
            {
                inputParameters.Append($"'{mainCriteria}'");
                if (appliedFilters.Any()/* || appliedTemplateParameters.Any()*/)
                {
                    inputParameters.Append(separator);
                }
            }
            if (appliedFilters/*.Concat(appliedTemplateParameters)*/.Any())
            {
                inputParameters.Append(string.Join(separator, appliedFilters/*.Concat(appliedTemplateParameters)*/));
            }
            return inputParameters.ToString();
        }

        /// <summary>
        /// Represents a table row for the template with the package information.
        /// </summary>
        private class SearchResultTableRow
        {
            internal SearchResultTableRow(TemplateGroupTableRow templateGroupTableRow, string packageName, long downloads = 0)
            {
                TemplateGroupInfo = templateGroupTableRow;
                PackageName = packageName;
                TotalDownloads = downloads;
            }

            internal string PackageName { get; private set; }

            internal string PrintableTotalDownloads
            {
                get
                {
                    if (TotalDownloads < 1)
                    {
                        return string.Empty;
                    }
                    else if (TotalDownloads < 1000)
                    {
                        return "<1k";
                    }
                    else
                    {
                        return $"{(TotalDownloads / 1000):N0}k";
                    }
                }
            }

            internal TemplateGroupTableRow TemplateGroupInfo { get; private set; }

            internal long TotalDownloads { get; private set; }
        }
    }
}
