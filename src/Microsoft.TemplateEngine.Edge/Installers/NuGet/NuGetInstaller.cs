using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    class NuGetInstaller : IInstaller
    {
        private readonly IInstallerFactory factory;
        private readonly IManagedTemplatesSourcesProvider provider;
        private readonly string installPath;
        private readonly IDownloader _packageDownloader;
        private readonly IUpdateChecker _updateChecker;
        private readonly IEngineEnvironmentSettings _environmentSettings;

        public NuGetInstaller(IInstallerFactory factory, IManagedTemplatesSourcesProvider provider, IEngineEnvironmentSettings settings, string installPath)
        {
            this.factory = factory;
            this.provider = provider;
            this.installPath = installPath;
            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(settings);
            _packageDownloader = packageManager;
            _updateChecker = packageManager;
            _environmentSettings = settings;
        }

        public string Name => factory.Name;

        public Guid FactoryId => factory.Id;

        public Task<bool> CanInstallAsync(InstallRequest installationRequest)
        {
            //TODO: Do better than this? This should be good enough as long as we only have Folder and NuGet installers...
            return Task.FromResult(!string.IsNullOrWhiteSpace(installationRequest.Identifier) && !Directory.Exists(installationRequest.Identifier));
        }

        public async Task<InstallResult> InstallAsync(InstallRequest installRequest)
        {
            try
            {
                string packageLocation;
                Dictionary<string, string> sourceDetails = new Dictionary<string, string>();
                if (IsLocalPackage(installRequest))
                {
                    packageLocation = Path.GetFileName(installRequest.Identifier);
                    _environmentSettings.Host.FileSystem.FileCopy(installRequest.Identifier, packageLocation, overwrite: true);
                    sourceDetails[NuGetManagedTemplatesSource.LocalPackageKey] = true.ToString();
                }
                else
                {
                    DownloadResult result = await _packageDownloader.DownloadPackageAsync(installRequest, installPath).ConfigureAwait(false);
                    packageLocation = result.FullPath;
                    sourceDetails[NuGetManagedTemplatesSource.AuthorKey] = result.Author;
                    sourceDetails[NuGetManagedTemplatesSource.NuGetSourceKey] = result.NuGetSource;
                    sourceDetails[NuGetManagedTemplatesSource.PackageIdKey] = result.PackageIdentifier;
                    sourceDetails[NuGetManagedTemplatesSource.PackageVersionKey] = result.PackageVersion.ToString();
                }
                NuGetManagedTemplatesSource source = new NuGetManagedTemplatesSource(provider, packageLocation, installRequest.Details);
                return InstallResult.CreateSuccess(source);
            }
            catch (DownloadException e)
            {
                return InstallResult.CreateFailure(InstallResult.ErrorCode.DownloadFailed, e.Message);
            }
            catch (PackageNotFoundException e)
            {
                return InstallResult.CreateFailure(InstallResult.ErrorCode.PackageNotFound, e.Message);
            }
            catch (InvalidNuGetSourceException e)
            {
                return InstallResult.CreateFailure(InstallResult.ErrorCode.InvalidSource, e.Message);
            }
            catch (Exception e)
            {
                return InstallResult.CreateFailure(InstallResult.ErrorCode.GenericError, $"Failed to install the package {installRequest.Identifier}, reason: {e.Message}");
            }
        }

        public Task<UninstallResult> UninstallAsync(IManagedTemplatesSource managedSource)
        {
            //Remove managedSource.MountPointUri
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<IManagedTemplatesSourceUpdate>> GetLatestVersionAsync(IEnumerable<IManagedTemplatesSource> sources)
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

        public (string mountPointUri, IReadOnlyDictionary<string, string> details) Serialize(IManagedTemplatesSource managedSource)
        {
            return (managedSource.MountPointUri, managedSource.Details);
        }

        private bool IsLocalPackage (InstallRequest installRequest)
        {
            return File.Exists(installRequest.Identifier);
        }
    }
}
