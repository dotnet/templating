using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    class NuGetInstaller : IInstaller
    {
        private readonly IInstallerFactory factory;
        private readonly IManagedTemplatesSourcesProvider provider;
        private readonly string installPath;

        public NuGetInstaller(IInstallerFactory factory, IManagedTemplatesSourcesProvider provider, IEngineEnvironmentSettings settings, string installPath)
        {
            this.factory = factory;
            this.provider = provider;
            this.installPath = installPath;
        }

        public string Name => factory.Name;

        public Guid FactoryId => factory.Id;

        public Task<bool> CanInstallAsync(InstallRequest installationRequest)
        {
            //TODO: Do better than this? This should be good enough as long as we only have Folder and NuGet installers...
            return Task.FromResult(!string.IsNullOrWhiteSpace(installationRequest.Identifier) && !Directory.Exists(installationRequest.Identifier));
        }

        public Task<InstallResult> InstallAsync(InstallRequest installRequest)
        {
            //var installNuPkgTo=Path.Combine(installPath,packageName));
            //if installRequest.Identifier is local .nupkg
            //File.Copy(installRequest.Identifier,installNuPkgTo);
            //return new NuGetManagedTemplatesSource(provider, installNuPkgTo, installRequest.Details)
            //else
            //Download .nupkg to installNuPkgTo
            //return new NuGetManagedTemplatesSource(provider, installNuPkgTo, installRequest.Details)
            throw new NotImplementedException();
        }

        public Task<UninstallResult> UninstallAsync(IManagedTemplatesSource managedSource)
        {
            //Remove managedSource.MountPointUri
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<IManagedTemplatesSourceUpdate>> GetLatestVersions(IEnumerable<IManagedTemplatesSource> sources)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<InstallResult>> UpdateAsync(IEnumerable<IManagedTemplatesSourceUpdate> sources)
        {
            throw new NotImplementedException();
        }

        public IManagedTemplatesSource Deserialize(IManagedTemplatesSourcesProvider provider, string mountPointUri, object details)
        {
            throw new NotImplementedException();
        }

        public (string mountPointUri, Dictionary<string, string> details) Serialize(IManagedTemplatesSource managedSource)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<string>> GetAutocompleteAsync(string textSoFar, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
