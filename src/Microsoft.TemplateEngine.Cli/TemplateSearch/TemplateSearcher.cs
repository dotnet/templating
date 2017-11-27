using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.CommandParsing;
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

        // Returns a dictionary whose:
        //  keys:   search source Id
        //  values: a dictionary whose:
        //      keys:   template pack name
        //      values: a list of matched templates in the pack
        public async Task<IReadOnlyList<TemplateSourceSearchResult>> SearchForTemplatesAsync()
        {
            string templateName = _commandInput.TemplateName;

            List<TemplateSourceSearchResult> matchesForAllSources = new List<TemplateSourceSearchResult>();

            if (string.IsNullOrEmpty(templateName))
            {
                return matchesForAllSources;
            }

            foreach (ITemplateSearchSource searchSource in _environmentSettings.SettingsLoader.Components.OfType<ITemplateSearchSource>())
            {
                TemplateSourceSearchResult matchesForSource = await GetBestMatchesForSourceAsync(searchSource, templateName);
                if (matchesForSource.PacksWithMatches.Count > 0)
                {
                    matchesForAllSources.Add(matchesForSource);
                }
            }

            return matchesForAllSources;
        }

        // TODO: if needed, tweak the return logic - we may want fewer, or different constraints on what is considered a "match" than is used for installed templates.
        private async Task<TemplateSourceSearchResult> GetBestMatchesForSourceAsync(ITemplateSearchSource searchSource, string templateName)
        {
            IReadOnlyList<ITemplateNameSearchResult> nameMatches = await searchSource.CheckForTemplateNameMatchesAsync(templateName);

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

            TemplateSourceSearchResult results = new TemplateSourceSearchResult(searchSource.Id, searchSource.DisplayName);

            if (templateMatches.Count == 0)
            {
                return results;
            }

            IReadOnlyDictionary<string, string> templateIdentityToPackMap = nameMatches.ToDictionary(x => x.Template.Identity, x => x.PackName);

            foreach (ITemplateMatchInfo match in templateMatches)
            {
                if (!templateIdentityToPackMap.TryGetValue(match.Info.Identity, out string packName))
                {
                    // this can't realistically happen. The templateMatches will always be a subset of the nameMatches, and thus will always be in the map.
                    throw new Exception("Unexpected error searching for templates");
                }

                results.AddMatchForPack(packName, match);
            }

            return results;
        }
    }
}
