using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge.Installers.Folder
{
    class FolderInstaller : IInstaller
    {
        public FolderInstaller(FolderInstallerFactory factory, IManagedTemplatesSourcesProvider provider)
        {
            Name = factory.Name;
            FactoryId = factory.Id;
        }

        public string Name { get; }

        public Guid FactoryId { get; }

        public Task<bool> CanInstallAsync(InstallRequest installationRequest)
        {
            return Task.FromResult(Directory.Exists(installationRequest.Identifier));
        }

        public IManagedTemplatesSource Deserialize(IManagedTemplatesSourcesProvider provider, string mountPointUri, object details)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<string>> GetAutocompleteAsync(string textSoFar, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<IManagedTemplatesSourceUpdate>> GetLatestVersions(IEnumerable<IManagedTemplatesSource> sources)
        {
            throw new NotImplementedException();
        }

        public Task<InstallResult> InstallAsync(InstallRequest installRequest)
        {
            throw new NotImplementedException();
        }

        public (string mountPointUri, Dictionary<string, string> details) Serialize(IManagedTemplatesSource managedSource)
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
