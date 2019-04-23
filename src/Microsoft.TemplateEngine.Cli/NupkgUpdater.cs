// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplateUpdates;
using Microsoft.TemplateEngine.Edge.TemplateUpdates;
using Microsoft.TemplateEngine.Cli.TemplateSearch;
using Microsoft.TemplateSearch.Common;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Cli
{
    internal class NupkgUpdater : IUpdater
    {
        public Guid Id { get; } = new Guid("DB5BF8D8-6181-496A-97DA-58616E135701");

        public Guid DescriptorFactoryId { get; } = NupkgInstallUnitDescriptorFactory.FactoryId;

        public string DisplayIdentifier { get; } = "Nupkg";

        private IEngineEnvironmentSettings _environmentSettings;
        private bool _isInitialized = false;
        private IReadOnlyList<ITemplateSearchSource> _templateSearchSourceList;

        public void Configure(IEngineEnvironmentSettings environmentSettings)
        {
            _environmentSettings = environmentSettings;
        }

        private async Task EnsureInitializedAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            List<ITemplateSearchSource> searchSourceList = new List<ITemplateSearchSource>();

            foreach (ITemplateSearchSource searchSource in _environmentSettings.SettingsLoader.Components.OfType<ITemplateSearchSource>())
            {
                try
                {
                    if (await searchSource.TryConfigureAsync(_environmentSettings))
                    {
                        searchSourceList.Add(searchSource);
                    }
                }
                catch (Exception ex)
                {
                    Reporter.Error.WriteLine($"Error configuring search source: {searchSource.DisplayName}.\r\nError = {ex.Message}");
                }
            }

            _templateSearchSourceList = searchSourceList;

            _isInitialized = true;
        }

        public async Task<IReadOnlyList<IUpdateUnitDescriptor>> CheckForUpdatesAsync(IReadOnlyList<IInstallUnitDescriptor> descriptorsToCheck)
        {
            await EnsureInitializedAsync();

            IReadOnlyDictionary<string, IInstallUnitDescriptor> installedPackToInstallDescriptorMap = descriptorsToCheck.ToDictionary(d => d.Identifier, d => d);

            List<IUpdateUnitDescriptor> updateList = new List<IUpdateUnitDescriptor>();

            foreach (ITemplateSearchSource searchSource in _templateSearchSourceList)
            {
                IReadOnlyDictionary<string, PackToTemplateEntry> candidateUpdatePackMatchList = await searchSource.CheckForTemplatePackMatchesAsync(installedPackToInstallDescriptorMap.Keys.ToList());

                foreach (KeyValuePair<string, PackToTemplateEntry> candidateUpdatePackMatch in candidateUpdatePackMatchList)
                {
                    string packName = candidateUpdatePackMatch.Key;
                    PackToTemplateEntry packToUpdate = candidateUpdatePackMatch.Value;

                    if (installedPackToInstallDescriptorMap.TryGetValue(packName, out IInstallUnitDescriptor installDescriptor)
                            && (installDescriptor is NupkgInstallUnitDescriptor nupkgInstallDescriptor)
                            && SemanticVersion.TryParse(nupkgInstallDescriptor.Version, out SemanticVersion nupkgVersion)
                            && SemanticVersion.TryParse(packToUpdate.Version, out SemanticVersion updateInfoVersion)
                            && updateInfoVersion > nupkgVersion)
                    {
                        IUpdateUnitDescriptor updateDescriptor = new UpdateUnitDescriptor(installDescriptor, packName, packName);
                        updateList.Add(updateDescriptor);
                    }
                }
            }

            return updateList;
        }

        public void ApplyUpdates(IInstaller installer, IReadOnlyList<IUpdateUnitDescriptor> updatesToApply)
        {
            IReadOnlyList<IUpdateUnitDescriptor> filteredUpdateToApply = updatesToApply.Where(x => x.InstallUnitDescriptor.FactoryId == DescriptorFactoryId).ToList();
            installer.InstallPackages(filteredUpdateToApply.Select(x => x.InstallString));
        }
    }
}
