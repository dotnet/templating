using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    internal class TemplateSearcher
    {
        public TemplateSearcher(IEngineEnvironmentSettings environmentSettings, INewCommandInput commandInput, string defaultLanguage)
        {
            _environmentSettings = environmentSettings;
            _commandInput = commandInput;
            _defaultLanguage = defaultLanguage;
        }

        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly INewCommandInput _commandInput;
        private readonly string _defaultLanguage;

        // Search all of the registered sources.
        public async Task<IReadOnlyList<TemplateSourceSearchResult>> SearchForTemplatesAsync(HashSet<string> packsToIgnore = null)
        {
            string inputTemplateName = _commandInput.TemplateName;

            List<TemplateSourceSearchResult> matchesForAllSources = new List<TemplateSourceSearchResult>();

            if (string.IsNullOrEmpty(inputTemplateName))
            {
                return matchesForAllSources;
            }

            foreach (ITemplateSearchSource searchSource in _environmentSettings.SettingsLoader.Components.OfType<ITemplateSearchSource>())
            {
                if (!searchSource.TryConfigure(_environmentSettings))
                {
                    continue;
                }

                TemplateSourceSearchResult matchesForSource = await GetBestMatchesForSourceAsync(searchSource, inputTemplateName, packsToIgnore);
                if (matchesForSource.PacksWithMatches.Count > 0)
                {
                    matchesForAllSources.Add(matchesForSource);
                }
            }

            return matchesForAllSources;
        }

        // If needed, tweak the return logic - we may want fewer, or different constraints on what is considered a "match" than is used for installed templates.
        private async Task<TemplateSourceSearchResult> GetBestMatchesForSourceAsync(ITemplateSearchSource searchSource, string templateName, HashSet<string> packsToIgnore)
        {
            IReadOnlyList<ITemplateNameSearchResult> nameMatches = await GetTemplateNameMatchesAsync(searchSource, templateName, packsToIgnore);
            IReadOnlyList<ITemplateMatchInfo> templateMatches = FilterMatchesOnParameters(nameMatches);

            TemplateSourceSearchResult results = new TemplateSourceSearchResult(searchSource.DisplayName);

            if (templateMatches.Count == 0)
            {
                return results;
            }

            // Map the identities of the templateMatches to the corresponding pack info
            HashSet<string> matchedTemplateIdentities = new HashSet<string>(templateMatches.Select(t => t.Info.Identity));
            IReadOnlyDictionary<string, PackAndVersion> templateIdentityToPackInfoMap = nameMatches.Where(m => matchedTemplateIdentities.Contains(m.Template.Identity))
                                                                                                    .ToDictionary(x => x.Template.Identity,
                                                                                                                  x => x.PackInfo);

            foreach (ITemplateMatchInfo match in templateMatches)
            {
                if (!templateIdentityToPackInfoMap.TryGetValue(match.Info.Identity, out PackAndVersion packInfo))
                {
                    // this can't realistically happen. The templateMatches will always be a subset of the nameMatches, and thus will always be in the map.
                    throw new Exception("Unexpected error searching for templates");
                }

                results.AddMatchForPack(packInfo, match);
            }

            return results;
        }

        private async Task<IReadOnlyList<ITemplateNameSearchResult>> GetTemplateNameMatchesAsync(ITemplateSearchSource searchSource, string templateName, HashSet<string> packsToIgnore)
        {
            IReadOnlyList<ITemplateNameSearchResult> nameMatches = await searchSource.CheckForTemplateNameMatchesAsync(templateName);
            IReadOnlyList<ITemplateNameSearchResult> filteredNameMatches;

            if (packsToIgnore != null && packsToIgnore.Count > 0)
            {
                filteredNameMatches = nameMatches.Where(match => !packsToIgnore.Contains(match.PackInfo.Name)).ToList();
            }
            else
            {
                filteredNameMatches = nameMatches;
            }

            return filteredNameMatches;
        }

        private IReadOnlyList<ITemplateMatchInfo> FilterMatchesOnParameters(IReadOnlyList<ITemplateNameSearchResult> nameMatches)
        {
            IHostSpecificDataLoader hostSpecificDataLoader = new InMemoryHostSpecificDataLoader(nameMatches.ToDictionary(x => x.Template.Identity, x => x.HostSpecificTemplateData));
            TemplateListResolutionResult templateResolutionResult = TemplateListResolver.GetTemplateResolutionResult(nameMatches.Select(x => x.Template).ToList(), hostSpecificDataLoader, _commandInput, _defaultLanguage);

            IReadOnlyList<ITemplateMatchInfo> templateMatches;
            if (templateResolutionResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyList<ITemplateMatchInfo> unambiguousMatches))
            {
                templateMatches = unambiguousMatches;
            }
            else
            {
                templateMatches = templateResolutionResult.GetBestTemplateMatchList();
            }

            return templateMatches;
        }
    }
}
