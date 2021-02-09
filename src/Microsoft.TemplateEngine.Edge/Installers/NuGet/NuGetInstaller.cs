// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class NuGetInstaller : IInstaller
    {
        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly IDownloader _packageDownloader;
        private readonly IUpdateChecker _updateChecker;
        private readonly IInstallerFactory factory;
        private readonly string installPath;
        public NuGetInstaller(IInstallerFactory factory, IManagedTemplatesSourcesProvider provider, IEngineEnvironmentSettings settings, string installPath)
        {
            this.factory = factory;
            this.Provider = provider;
            this.installPath = installPath;
            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(settings);
            _packageDownloader = packageManager;
            _updateChecker = packageManager;
            _environmentSettings = settings;
        }

        public Guid FactoryId => factory.Id;
        public string Name => factory.Name;
        public IManagedTemplatesSourcesProvider Provider { get; }

        public Task<bool> CanInstallAsync(InstallRequest installationRequest)
        {
            //TODO: Do better than this? This should be good enough as long as we only have Folder and NuGet installers...
            return Task.FromResult(!string.IsNullOrWhiteSpace(installationRequest.Identifier) && !Directory.Exists(installationRequest.Identifier));
        }

        public IManagedTemplatesSource Deserialize(IManagedTemplatesSourcesProvider provider, string mountPointUri, object details)
        {
            return new NuGetManagedTemplatesSource(this, mountPointUri, details as Dictionary<string, string>);
        }

        public async Task<IReadOnlyList<ManagedTemplatesSourceUpdate>> GetLatestVersionAsync(IEnumerable<IManagedTemplatesSource> sources)
        {
            _ = sources ?? throw new ArgumentNullException(nameof(sources));
            return await Task.WhenAll(sources.Select(source =>
                {
                    if (source is NuGetManagedTemplatesSource nugetSource && !nugetSource.LocalPackage)
                    {
                        return _updateChecker.GetLatestVersionAsync(nugetSource);
                    }
                    else
                    {
                        return Task.FromResult(new ManagedTemplatesSourceUpdate(source, source.Version));
                    }
                })).ConfigureAwait(false);
        }

        public async Task<InstallResult> InstallAsync(InstallRequest installRequest)
        {
            _ = installRequest ?? throw new ArgumentNullException(nameof(installRequest));

            try
            {
                string packageLocation;
                Dictionary<string, string> sourceDetails = new Dictionary<string, string>();
                if (IsLocalPackage(installRequest))
                {
                    packageLocation = Path.Combine(installPath, Path.GetFileName(installRequest.Identifier));
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
                NuGetManagedTemplatesSource source = new NuGetManagedTemplatesSource(this, packageLocation, sourceDetails);
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

        public (string mountPointUri, IReadOnlyDictionary<string, string> details) Serialize(IManagedTemplatesSource managedSource)
        {
            return (managedSource.MountPointUri, managedSource.Details);
        }

        public Task<UninstallResult> UninstallAsync(IManagedTemplatesSource managedSource)
        {
            _ = managedSource ?? throw new ArgumentNullException(nameof(managedSource));
            if (!(managedSource is NuGetManagedTemplatesSource))
            {
                return Task.FromResult(UninstallResult.CreateFailure($"{managedSource.Identifier} is not supported by {Name}"));
            }
            try
            {
                _environmentSettings.Host.FileSystem.FileDelete(managedSource.MountPointUri);
                return Task.FromResult(UninstallResult.CreateSuccess());
            }
            catch (Exception ex)
            {
                //TODO: log issue when logger is available
                return Task.FromResult(UninstallResult.CreateFailure($"Failed to uninstall {managedSource.Identifier}, reason: {ex.Message}"));
            }
        }
        public Task<IReadOnlyList<InstallResult>> UpdateAsync(IEnumerable<ManagedTemplatesSourceUpdate> sources)
        {
            _ = sources ?? throw new ArgumentNullException(nameof(sources));
            throw new NotImplementedException();
        }
        private bool IsLocalPackage(InstallRequest installRequest)
        {
            return File.Exists(installRequest.Identifier);
        }
    }
}
