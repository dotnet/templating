using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge.Installers.Folder
{
    class FolderInstaller : IInstaller
    {
        private readonly IEngineEnvironmentSettings settings;

        public FolderInstaller(IEngineEnvironmentSettings settings, FolderInstallerFactory factory, IManagedTemplatesSourcesProvider provider)
        {
            Name = factory.Name;
            FactoryId = factory.Id;
            this.settings = settings;
            this.Provider = provider;
        }

        public string Name { get; }

        public Guid FactoryId { get; }

        public IManagedTemplatesSourcesProvider Provider { get; }

        public Task<bool> CanInstallAsync(InstallRequest installationRequest)
        {
            return Task.FromResult(Directory.Exists(installationRequest.Identifier));
        }

        public IManagedTemplatesSource Deserialize(IManagedTemplatesSourcesProvider provider, string mountPointUri, object details)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<IManagedTemplatesSourceUpdate>> GetLatestVersionAsync(IEnumerable<IManagedTemplatesSource> sources)
        {
            throw new NotImplementedException();
        }

        public Task<InstallResult> InstallAsync(InstallRequest installRequest)
        {
            throw new NotImplementedException();
        }

        public (string mountPointUri, IReadOnlyDictionary<string, string> details) Serialize(IManagedTemplatesSource managedSource)
        {
            throw new NotImplementedException();
        }

        public Task<UninstallResult> UninstallAsync(IManagedTemplatesSource managedSource)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<InstallResult>> UpdateAsync(IEnumerable<IManagedTemplatesSourceUpdate> sources)
        {
            throw new NotImplementedException();
        }
    }
}
