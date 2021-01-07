using Microsoft.TemplateEngine.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Utils
{
    class UserInstalledPackagesFactory : ITemplatesInstallSourcesProviderFactory
    {
        public static readonly Guid FactoryId = new Guid("{3AACE22E-E978-4BAF-8BC1-568B290A238C}");

        public Guid Id => FactoryId;

        public ITemplatesInstallSourcesProvider CreateProvider(IEngineEnvironmentSettings settings)
        {
            return new UserInstalledPackages(settings);
        }

        internal class UserInstalledPackages : ITemplatesInstallSourcesProvider
        {
            private IEngineEnvironmentSettings settings;

            public UserInstalledPackages(IEngineEnvironmentSettings settings)
            {
                this.settings = settings;
            }

            public Task<List<TemplatesInstallSource>> GetInstalledPackagesAsync(CancellationToken cancellationToken)
            {
                var list = new List<TemplatesInstallSource>();
                var userSettings = settings.SettingsLoader.GlobalSettings;
                foreach (var install in userSettings.UserInstalledTemplatesSources)
                {
                    list.Add(new TemplatesInstallSource()
                    {
                        LastWriteTime = install.InstallTime,
                        MountPointFactoryId = install.MountPointFactoryId,
                        Place = install.Place,
                    });
                }
                return Task.FromResult(list);
            }
        }
    }
}
