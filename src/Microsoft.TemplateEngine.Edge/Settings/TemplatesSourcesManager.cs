using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    class TemplatesSourcesManager : ITemplatesSourcesManager
    {
        private readonly IEngineEnvironmentSettings environmentSettings;

        //TODO: Handle threadsafey and maybe improve perf

        Dictionary<ITemplatesSourcesProvider, Task<IReadOnlyList<ITemplatesSource>>> cachedSources;

        public TemplatesSourcesManager(IEngineEnvironmentSettings environmentSettings)
        {
            this.environmentSettings = environmentSettings;
        }

        private void EnsureLoaded()
        {
            if (cachedSources != null)
                return;
            cachedSources = new Dictionary<ITemplatesSourcesProvider, Task<IReadOnlyList<ITemplatesSource>>>();
            var providers = environmentSettings.SettingsLoader.Components.OfType<ITemplatesSourcesProviderFactory>().Select(f => f.CreateProvider(environmentSettings));
            foreach (var provider in providers)
            {
                provider.SourcesChanged += async () =>
                {
                    cachedSources[provider] = provider.GetAllSourcesAsync(default);
                    SourcesChanged?.Invoke();
                };
                cachedSources[provider] = Task.Run(() => provider.GetAllSourcesAsync(default));
            }
        }

        public event Action SourcesChanged;

        public IManagedTemplatesSourcesProvider GetManagedProvider(string name)
        {
            EnsureLoaded();
            return cachedSources.Keys.OfType<IManagedTemplatesSourcesProvider>().FirstOrDefault(p => p.Factory.Name == name);
        }

        public IManagedTemplatesSourcesProvider GetManagedProvider(Guid id)
        {
            EnsureLoaded();
            return cachedSources.Keys.OfType<IManagedTemplatesSourcesProvider>().FirstOrDefault(p => p.Factory.Id == id);
        }

        public async Task<IReadOnlyList<(IManagedTemplatesSourcesProvider Provider, IReadOnlyList<IManagedTemplatesSource> ManagedSources)>> GetManagedSourcesGroupedByProvider(bool force = false)
        {
            EnsureLoaded();
            var sources = await GetManagedTemplatesSources(force).ConfigureAwait(false);
            var list = new List<(IManagedTemplatesSourcesProvider Provider, IReadOnlyList<IManagedTemplatesSource> ManagedSources)>();
            foreach (var source in sources.GroupBy(s => s.Installer.Provider))
            {
                list.Add((source.Key, source.ToList()));
            }
            return list;
        }

        public async Task<IReadOnlyList<IManagedTemplatesSource>> GetManagedTemplatesSources(bool force = false)
        {
            EnsureLoaded();
            return (await GetTemplatesSources(force).ConfigureAwait(false)).OfType<IManagedTemplatesSource>().ToList();
        }

        public async Task<IReadOnlyList<ITemplatesSource>> GetTemplatesSources(bool force)
        {
            EnsureLoaded();
            if (force)
            {
                foreach (var provider in cachedSources.Keys)
                {
                    cachedSources[provider] = Task.Run(() => provider.GetAllSourcesAsync(default));
                }
            }

            var sources = new List<ITemplatesSource>();
            foreach (var task in cachedSources.Values)
            {
                sources.AddRange(await task);
            }
            return sources;
        }
    }
}
