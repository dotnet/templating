// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplateFiltering;
using Microsoft.TemplateEngine.Abstractions.TemplatePackage;

namespace Microsoft.TemplateSearch.Common
{
    public class TemplateSearchCoordinator
    {
        private bool _isSearchPerformed;

        public TemplateSearchCoordinator(IEngineEnvironmentSettings environmentSettings, string inputTemplateName, string defaultLanguage, Func<IReadOnlyList<ITemplateNameSearchResult>, IReadOnlyList<ITemplateMatchInfo>> matchFilter)
        {
            EnvironmentSettings = environmentSettings;
            InputTemplateName = inputTemplateName;
            DefaultLanguage = defaultLanguage;
            MatchFilter = matchFilter;
            _isSearchPerformed = false;
        }

        protected IEngineEnvironmentSettings EnvironmentSettings { get; }

        protected string InputTemplateName { get; }

        protected string DefaultLanguage { get; }

        protected Func<IReadOnlyList<ITemplateNameSearchResult>, IReadOnlyList<ITemplateMatchInfo>> MatchFilter { get; set; }

        protected SearchResults SearchResults { get; set; }

        public async Task<SearchResults> SearchAsync(IReadOnlyList<ITemplatePackage> existingTemplatePackages, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureSearchResultsAsync(existingTemplatePackages.OfType<IManagedTemplatePackage>().ToArray(), cancellationToken).ConfigureAwait(false);
            return SearchResults;
        }

        protected async Task EnsureSearchResultsAsync(IReadOnlyList<IManagedTemplatePackage> existingTemplatePackages, CancellationToken cancellationToken)
        {
            if (_isSearchPerformed)
            {
                return;
            }

            TemplateSearcher searcher = new TemplateSearcher(EnvironmentSettings, DefaultLanguage, MatchFilter);

            SearchResults = await searcher.SearchForTemplatesAsync(existingTemplatePackages, InputTemplateName, cancellationToken).ConfigureAwait(false);

            _isSearchPerformed = true;
        }
    }
}
