using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateSearch.Common
{
    public class TemplateSearchCoordinator
    {
        public TemplateSearchCoordinator(IEngineEnvironmentSettings environmentSettings, string inputTemplateName, string defaultLanguage, Func<IReadOnlyList<ITemplateNameSearchResult>, IReadOnlyList<ITemplateMatchInfo>> matchFilter)
        {
            _environmentSettings = environmentSettings;
            _inputTemplateName = inputTemplateName;
            _defaultLanguage = defaultLanguage;
            _matchFilter = matchFilter;
            _isSearchPerformed = false;
        }

        protected readonly IEngineEnvironmentSettings _environmentSettings;
        protected readonly string _inputTemplateName;
        protected readonly string _defaultLanguage;
        protected Func<IReadOnlyList<ITemplateNameSearchResult>, IReadOnlyList<ITemplateMatchInfo>> _matchFilter;
        private bool _isSearchPerformed;
        protected SearchResults _searchResults;

        public async Task<SearchResults> SearchAsync()
        {
            await EnsureSearchResultsAsync();

            return _searchResults;
        }

        protected async Task EnsureSearchResultsAsync()
        {
            if (_isSearchPerformed)
            {
                return;
            }

            TemplateSearcher searcher = new TemplateSearcher(_environmentSettings, _defaultLanguage, _matchFilter);
            IReadOnlyList<IManagedTemplatesSource> existingInstallDescriptors;

            if (_environmentSettings.SettingsLoader is SettingsLoader settingsLoader)
            {
                existingInstallDescriptors = (await settingsLoader.TemplatesSourcesManager.GetTemplatesSources(false)).OfType<IManagedTemplatesSource>().ToList();
            }
            else
            {
                existingInstallDescriptors = new List<IManagedTemplatesSource>();
            }

            _searchResults = await searcher.SearchForTemplatesAsync(existingInstallDescriptors, _inputTemplateName);

            _isSearchPerformed = true;
        }
    }
}
