using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    internal class DisplayOnlyTemplateSearchCoordinator : ITemplateSearchCoordinator
    {
        public DisplayOnlyTemplateSearchCoordinator(IEngineEnvironmentSettings environmentSettings, INewCommandInput commandInput, string defaultLanguage)
        {
            _environmentSettings = environmentSettings;
            _commandInput = commandInput;
            _defaultLanguage = defaultLanguage;
        }

        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly INewCommandInput _commandInput;
        private readonly string _defaultLanguage;

        public async Task CoordinateAsync()
        {
            TemplateSearcher searcher = new TemplateSearcher(_environmentSettings, _commandInput, _defaultLanguage);
            HashSet<string> alreadyInstalledPacks;

            // filter the already installed packs
            if (_environmentSettings.SettingsLoader is SettingsLoader settingsLoader)
            {
                alreadyInstalledPacks = new HashSet<string>(settingsLoader.InstallUnitDescriptorCache.InstalledItems.Values);
            }
            else
            {
                alreadyInstalledPacks = new HashSet<string>();
            }

            IReadOnlyList<TemplateSourceSearchResult> searchResults = await searcher.SearchForTemplatesAsync(alreadyInstalledPacks);

            DisplaySearchResults(searchResults);
        }

        public void DisplaySearchResults(IReadOnlyList<TemplateSourceSearchResult> searchResultList)
        {
            if (searchResultList.Count == 0)
            {
                Reporter.Output.WriteLine("No matches found for the search criteria.");
                return;
            }

            foreach (TemplateSourceSearchResult sourceResult in searchResultList)
            {
                string sourceHeader;

                if (sourceResult.PacksWithMatches.Values.Any(match => match.TemplateMatches.Any(t => t.IsInvokableMatch())))
                {
                    sourceHeader = string.Format("Matches from template source: {0}", sourceResult.SourceDisplayName);
                }
                else
                {
                    sourceHeader = string.Format("*** Partial *** matches from template source: {0}", sourceResult.SourceDisplayName);
                }

                Reporter.Output.WriteLine(sourceHeader);
                Reporter.Output.WriteLine(new string('-', sourceHeader.Length));

                foreach (TemplatePackSearchResult matchesForPack in sourceResult.PacksWithMatches.Values)
                {
                    DisplayResultsForPack(matchesForPack);
                    Reporter.Output.WriteLine();
                }
            }
        }

        public void DisplayResultsForPack(TemplatePackSearchResult matchesForPack)
        {
            string fullyQualifiedPackName = $"{matchesForPack.PackInfo.Name}::{matchesForPack.PackInfo.Version}";

            Reporter.Output.WriteLine(string.Format("Install Command: dotnet {0} -i {1}", _commandInput.CommandName, fullyQualifiedPackName));

            foreach (ITemplateMatchInfo templateMatch in matchesForPack.TemplateMatches)
            {
                Reporter.Output.WriteLine(string.Format("\tTemplate Name: {0}", templateMatch.Info.Name));

                IReadOnlyList<ParameterMatchDisposition> parameterMatchDispositions = ParameterMatchDisposition.FromTemplateMatchInfo(templateMatch);

                if (parameterMatchDispositions.Any(d => d.NameDisposition != ParameterNameDisposition.Exact || d.ValueDisposition != ParameterValueDisposition.Valid))
                {
                    foreach (ParameterMatchDisposition disposition in parameterMatchDispositions)
                    {
                        if (disposition.NameDisposition == ParameterNameDisposition.Exact && disposition.ValueDisposition == ParameterValueDisposition.Valid)
                        {
                            continue;
                        }

                        if (disposition.NameDisposition == ParameterNameDisposition.Invalid)
                        {
                            Reporter.Output.WriteLine(string.Format("\t\tParameter '{0}' is not valid for this template", disposition.Name));
                        }
                        else
                        {
                            string parameterInputFormat = _commandInput.TemplateParamInputFormat(disposition.Name);

                            if (disposition.ValueDisposition == ParameterValueDisposition.Ambiguous)
                            {
                                Reporter.Output.WriteLine(string.Format("\t\tParameter '{0}' has more than one choice value that starts with '{1}'", parameterInputFormat, disposition.Value));
                            }
                            else if (disposition.ValueDisposition == ParameterValueDisposition.Mismatch)
                            {
                                Reporter.Output.WriteLine(string.Format("\t\tParameter '{0}' does not accept a choice value of '{1}'", parameterInputFormat, disposition.Value));
                            }
                        }
                    }
                }
            }
        }
    }
}
