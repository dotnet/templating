using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    internal class TemplateSearchCoordinator
    {
        public TemplateSearchCoordinator(IEngineEnvironmentSettings environmentSettings, IInstaller installer, INewCommandInput commandInput, string defaultLanguage)
        {
            _environmentSettings = environmentSettings;
            _installer = installer;
            _commandInput = commandInput;
            _defaultLanguage = defaultLanguage;
        }

        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly IInstaller _installer;
        private readonly INewCommandInput _commandInput;
        private readonly string _defaultLanguage;

        public async Task CoordinateSearchAndInstallation(Func<string> inputGetter)
        {
            TemplateSearcher searcher = new TemplateSearcher(_environmentSettings, _commandInput, _defaultLanguage);
            IReadOnlyList<TemplateSourceSearchResult> searchResults = await searcher.SearchForTemplatesAsync();

            IReadOnlyList<TemplatePackSearchResult> packsToInstall = GetUserInstallChoices(searchResults, inputGetter);
            IReadOnlyList<string> packNamesToInstall = packsToInstall.Select(x => x.PackName).ToList();
            _installer.InstallPackages(packNamesToInstall);
        }

        private IReadOnlyList<TemplatePackSearchResult> GetUserInstallChoices(IReadOnlyList<TemplateSourceSearchResult> searchResults, Func<string> inputGetter)
        {
            if (searchResults.Count == 0)
            {
                Reporter.Output.WriteLine("No matches found for the search criteria.");
                return new List<TemplatePackSearchResult>();
            }

            HashSet<int> indexesToInstall = new HashSet<int>();
            bool done = false;
            IReadOnlyList<TemplatePackSearchResult> indexToPackLookup;

            do
            {
                indexToPackLookup = DisplayMatches(searchResults, indexesToInstall);
                Reporter.Output.WriteLine("Note: template packs may contain additional templates that didn't match");
                Reporter.Output.WriteLine("Options:");
                Reporter.Output.WriteLine("\tEnter one or more indexes to install, comma separated.");
                Reporter.Output.WriteLine("\t[I]nstall chosen template packs.");
                Reporter.Output.WriteLine("\t[C]ancel.");
                if (indexesToInstall.Count > 0)
                {
                    Reporter.Output.WriteLine("\t[R]eset selections.");
                    Reporter.Output.WriteLine("Packs preceeded by '*' have been chosen to be installed.");
                }

                string userChoice = inputGetter();

                switch (userChoice.ToLowerInvariant())
                {
                    case "i":
                    case "install":
                        done = true;
                        break;
                    case "c":
                    case "cancel":
                        indexesToInstall.Clear();
                        done = true;
                        break;
                    case "r":
                    case "reset":
                        indexesToInstall.Clear();
                        break;
                    default:
                        if (TryParseIndexList(userChoice, out IReadOnlyList<int> selectedIndexes))
                        {
                            List<int> invalidIndexes = selectedIndexes.Where(x => x < 0 || x > indexToPackLookup.Count).ToList();
                            if (invalidIndexes.Count > 0)
                            {
                                Reporter.Error.WriteLine(string.Format("One or more input indexes are out of range. Invalid indexes: {0}", string.Join(", ", invalidIndexes)).Bold().Red());
                            }
                            else
                            {
                                indexesToInstall.UnionWith(selectedIndexes);
                            }
                        }
                        else
                        {
                            Reporter.Error.WriteLine("Invalid choice.".Bold().Red());
                        }
                        break;
                }
            } while (!done);

            List<TemplatePackSearchResult> packsToInstall = new List<TemplatePackSearchResult>();
            foreach (int packIndex in indexesToInstall)
            {
                packsToInstall.Add(indexToPackLookup[packIndex]);
            }

            return packsToInstall;
        }

        private IReadOnlyList<TemplatePackSearchResult> DisplayMatches(IReadOnlyList<TemplateSourceSearchResult> searchResults, HashSet<int> indexesBeingInstalled)
        {
            int packIndex = 0;
            List<TemplatePackSearchResult> indexToPackLookup = new List<TemplatePackSearchResult>();

            foreach (TemplateSourceSearchResult sourceResult in searchResults)
            {
                Reporter.Output.WriteLine(string.Format("Matches from template source: {0}", sourceResult.SourceDisplayName));

                foreach (TemplatePackSearchResult matchesForPack in sourceResult.PacksWithMatches.Values)
                {
                    if (indexesBeingInstalled.Contains(packIndex))
                    {
                        Reporter.Output.Write("* ");
                    }

                    Reporter.Output.WriteLine(string.Format("\t{0}) Pack name: {1} --- matched templates:", packIndex, matchesForPack.PackName));
                    indexToPackLookup.Add(matchesForPack);

                    foreach (ITemplateMatchInfo templateMatch in matchesForPack.TemplateMatches)
                    {
                        Reporter.Output.WriteLine(string.Format("\t\t{0}", templateMatch.Info.Name));
                    }
                }

                packIndex++;
            }

            return indexToPackLookup;
        }

        // Attempts to parse the input as a comma separated list of integers.
        // If all entries are ints return true, false otherwise.
        private bool TryParseIndexList(string toParse, out IReadOnlyList<int> selectedIndexes)
        {
            List<int> parsedIndexes = new List<int>();

            List<string> rawIndexList = toParse.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (string rawIndex in rawIndexList)
            {
                if (Int32.TryParse(rawIndex, out int parsed))
                {
                    parsedIndexes.Add(parsed);
                }
                else
                {
                    selectedIndexes = null;
                    return false;
                }
            }

            selectedIndexes = parsedIndexes;
            return true;
        }
    }
}
