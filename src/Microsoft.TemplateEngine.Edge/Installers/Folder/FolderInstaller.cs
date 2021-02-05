using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            return new FolderManagedTemplatesSource(settings, this, mountPointUri);
        }

        public Task<IReadOnlyList<ManagedTemplatesSourceUpdate>> GetLatestVersionAsync(IEnumerable<IManagedTemplatesSource> sources)
        {
            return Task.FromResult<IReadOnlyList<ManagedTemplatesSourceUpdate>>(sources.Select(s => new ManagedTemplatesSourceUpdate(s, null)).ToList());
        }

        public Task<InstallResult> InstallAsync(InstallRequest installRequest)
        {
            if (Directory.Exists(installRequest.Identifier))
                return Task.FromResult(InstallResult.CreateSuccess(new FolderManagedTemplatesSource(settings, this, installRequest.Identifier)));
            else
                return Task.FromResult(InstallResult.CreateFailure(InstallResult.ErrorCode.GenericError, null));
        }

        public (string mountPointUri, IReadOnlyDictionary<string, string> details) Serialize(IManagedTemplatesSource managedSource)
        {
            return (managedSource.MountPointUri, null);
        }

        public Task<UninstallResult> UninstallAsync(IManagedTemplatesSource managedSource)
        {
            return Task.FromResult(UninstallResult.CreateSuccess());
        }

        public Task<IReadOnlyList<InstallResult>> UpdateAsync(IEnumerable<ManagedTemplatesSourceUpdate> sources)
        {
            return Task.FromResult<IReadOnlyList<InstallResult>>(new List<InstallResult>(0));
        }
    }
}
