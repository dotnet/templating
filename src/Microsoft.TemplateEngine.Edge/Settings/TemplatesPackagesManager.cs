using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatesPackages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    class TemplatesPackagesManager : ITemplatesPackagesManager
    {
        private readonly IEngineEnvironmentSettings environmentSettings;

        //TODO: Handle threadsafey and maybe improve perf

        Dictionary<ITemplatesPackagesProvider, Task<IReadOnlyList<ITemplatesPackage>>> cachedSources;

        public TemplatesPackagesManager(IEngineEnvironmentSettings environmentSettings)
        {
            this.environmentSettings = environmentSettings;
        }

        private void EnsureLoaded()
        {
            if (cachedSources != null)
                return;
            cachedSources = new Dictionary<ITemplatesPackagesProvider, Task<IReadOnlyList<ITemplatesPackage>>>();
            var providers = environmentSettings.SettingsLoader.Components.OfType<ITemplatesPackagesProviderFactory>().Select(f => f.CreateProvider(environmentSettings));
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

        public IManagedTemplatesPackagesProvider GetManagedProvider(string name)
        {
            EnsureLoaded();
            return cachedSources.Keys.OfType<IManagedTemplatesPackagesProvider>().FirstOrDefault(p => p.Factory.Name == name);
        }

        public IManagedTemplatesPackagesProvider GetManagedProvider(Guid id)
        {
            EnsureLoaded();
            return cachedSources.Keys.OfType<IManagedTemplatesPackagesProvider>().FirstOrDefault(p => p.Factory.Id == id);
        }

        public async Task<IReadOnlyList<(IManagedTemplatesPackagesProvider Provider, IReadOnlyList<IManagedTemplatesPackage> ManagedSources)>> GetManagedSourcesGroupedByProvider(bool force = false)
        {
            EnsureLoaded();
            var sources = await GetManagedTemplatesPackages(force).ConfigureAwait(false);
            var list = new List<(IManagedTemplatesPackagesProvider Provider, IReadOnlyList<IManagedTemplatesPackage> ManagedSources)>();
            foreach (var source in sources.GroupBy(s => s.ManagedProvider))
            {
                list.Add((source.Key, source.ToList()));
            }
            return list;
        }

        public async Task<IReadOnlyList<IManagedTemplatesPackage>> GetManagedTemplatesPackages(bool force = false)
        {
            EnsureLoaded();
            return (await GetTemplatesPackages(force).ConfigureAwait(false)).OfType<IManagedTemplatesPackage>().ToList();
        }

        public async Task<IReadOnlyList<ITemplatesPackage>> GetTemplatesPackages(bool force)
        {
            EnsureLoaded();
            if (force)
            {
                foreach (var provider in cachedSources.Keys)
                {
                    cachedSources[provider] = Task.Run(() => provider.GetAllSourcesAsync(default));
                }
            }

            var sources = new List<ITemplatesPackage>();
            foreach (var task in cachedSources.Values)
            {
                sources.AddRange(await task);
            }
            return sources;
        }
    }
}
