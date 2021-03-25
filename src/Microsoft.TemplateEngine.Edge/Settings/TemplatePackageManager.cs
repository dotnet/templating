// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatePackage;
using Microsoft.TemplateEngine.Edge.BuiltInManagedProvider;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    internal class TemplatePackageManager : ITemplatePackageManager, IDisposable
    {
        private readonly IEngineEnvironmentSettings environmentSettings;

        Dictionary<ITemplatePackageProvider, Task<IReadOnlyList<ITemplatePackage>>> cachedSources;

        public TemplatePackageManager(IEngineEnvironmentSettings environmentSettings)
        {
            this.environmentSettings = environmentSettings;
        }

        private void EnsureLoaded()
        {
            if (cachedSources != null)
            {
                return;
            }

            cachedSources = new Dictionary<ITemplatePackageProvider, Task<IReadOnlyList<ITemplatePackage>>>();
            var providers = environmentSettings.SettingsLoader.Components.OfType<ITemplatePackageProviderFactory>().Select(f => f.CreateProvider(environmentSettings));
            foreach (var provider in providers)
            {
                provider.SourcesChanged += () =>
                {
                    cachedSources[provider] = provider.GetAllSourcesAsync(default);
                    SourcesChanged?.Invoke();
                };
                cachedSources[provider] = Task.Run(() => provider.GetAllSourcesAsync(default));
            }
        }

        public event Action SourcesChanged;

        public IManagedTemplatePackageProvider GetManagedProvider(string name)
        {
            EnsureLoaded();
            return cachedSources.Keys.OfType<IManagedTemplatePackageProvider>().FirstOrDefault(p => p.Factory.Name == name);
        }

        public IManagedTemplatePackageProvider GetManagedProvider(Guid id)
        {
            EnsureLoaded();
            return cachedSources.Keys.OfType<IManagedTemplatePackageProvider>().FirstOrDefault(p => p.Factory.Id == id);
        }

        public async Task<IReadOnlyList<IManagedTemplatePackage>> GetManagedTemplatePackages(bool force = false)
        {
            EnsureLoaded();
            return (await GetTemplatePackages(force).ConfigureAwait(false)).OfType<IManagedTemplatePackage>().ToList();
        }

        public async Task<IReadOnlyList<ITemplatePackage>> GetTemplatePackages(bool force)
        {
            EnsureLoaded();
            if (force)
            {
                foreach (var provider in cachedSources.Keys)
                {
                    cachedSources[provider] = Task.Run(() => provider.GetAllSourcesAsync(default));
                }
            }

            var sources = new List<ITemplatePackage>();
            foreach (var task in cachedSources.Values)
            {
                sources.AddRange(await task);
            }
            return sources;
        }

        public void Dispose()
        {
            if (cachedSources == null)
            {
                return;
            }
            foreach (var provider in cachedSources.Keys.OfType<IDisposable>())
            {
                provider.Dispose();
            }
        }

        public IManagedTemplatePackageProvider GetBuiltInManagedProvider(InstallationScope scope = InstallationScope.Global)
        {
            switch (scope)
            {
                case InstallationScope.Global:
                    return GetManagedProvider(GlobalSettingsTemplatePackageProviderFactory.FactoryId);
            }
            return GetManagedProvider(GlobalSettingsTemplatePackageProviderFactory.FactoryId);
        }
    }
}
