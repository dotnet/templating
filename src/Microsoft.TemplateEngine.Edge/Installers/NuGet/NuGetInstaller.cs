// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.GlobalSettings;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class NuGetInstaller : IInstaller
    {
        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly IInstallerFactory _factory;
        private readonly string _installPath;
        private readonly IDownloader _packageDownloader;
        private readonly IUpdateChecker _updateChecker;

        public NuGetInstaller(IInstallerFactory factory, IManagedTemplatesSourcesProvider provider, IEngineEnvironmentSettings settings, string installPath)
        {
            _factory = factory;
            Provider = provider;
            _installPath = installPath;
            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(settings);
            _packageDownloader = packageManager;
            _updateChecker = packageManager;
            _environmentSettings = settings;
        }

        public Guid FactoryId => _factory.Id;
        public string Name => _factory.Name;
        public IManagedTemplatesSourcesProvider Provider { get; }

        public Task<bool> CanInstallAsync(InstallRequest installationRequest)
        {
            //TODO: Do better than this? This should be good enough as long as we only have Folder and NuGet installers...
            return Task.FromResult(!string.IsNullOrWhiteSpace(installationRequest.Identifier) && !Directory.Exists(installationRequest.Identifier));
        }

        public IManagedTemplatesSource Deserialize(IManagedTemplatesSourcesProvider provider, TemplatesSourceData data)
        {
            return new NuGetManagedTemplatesSource(_environmentSettings, this, data.MountPointUri, data.Details);
        }

        public async Task<IReadOnlyList<CheckUpdateResult>> GetLatestVersionAsync(IEnumerable<IManagedTemplatesSource> sources)
        {
            _ = sources ?? throw new ArgumentNullException(nameof(sources));
            return await Task.WhenAll(sources.Select(source =>
                {
                    if (source is NuGetManagedTemplatesSource nugetSource)
                    {
                        if (nugetSource.LocalPackage)
                        {
                            //updates for locally installed packages are not supported
                            return Task.FromResult(CheckUpdateResult.CreateSuccessNoUpdate(source));
                        }
                        try
                        {
                            return _updateChecker.GetLatestVersionAsync(nugetSource);
                        }
                        catch (PackageNotFoundException e)
                        {
                            return Task.FromResult(CheckUpdateResult.CreateFailure(source, InstallerErrorCode.PackageNotFound, e.Message));
                        }
                        catch (InvalidNuGetSourceException e)
                        {
                            return Task.FromResult(CheckUpdateResult.CreateFailure(source, InstallerErrorCode.InvalidSource, e.Message));
                        }
                        catch (Exception e)
                        {
                            return Task.FromResult(CheckUpdateResult.CreateFailure(source, InstallerErrorCode.GenericError, $"Failed to check the update for the package {source.Identifier}, reason: {e.Message}"));
                        }
                    }
                    else
                    {
                        return Task.FromResult(CheckUpdateResult.CreateFailure(source, InstallerErrorCode.UnsupportedRequest, $"source {source.Identifier} is not supported by installer {Name}"));
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
                    packageLocation = Path.Combine(_installPath, Path.GetFileName(installRequest.Identifier));
                    _environmentSettings.Host.FileSystem.FileCopy(installRequest.Identifier, packageLocation, overwrite: true);
                    sourceDetails[NuGetManagedTemplatesSource.LocalPackageKey] = true.ToString();
                    sourceDetails[NuGetManagedTemplatesSource.PackageIdKey] = installRequest.Identifier;
                }
                else
                {
                    DownloadResult result = await _packageDownloader.DownloadPackageAsync(installRequest, _installPath).ConfigureAwait(false);
                    packageLocation = result.FullPath;
                    sourceDetails[NuGetManagedTemplatesSource.AuthorKey] = result.Author;
                    sourceDetails[NuGetManagedTemplatesSource.NuGetSourceKey] = result.NuGetSource;
                    sourceDetails[NuGetManagedTemplatesSource.PackageIdKey] = result.PackageIdentifier;
                    sourceDetails[NuGetManagedTemplatesSource.PackageVersionKey] = result.PackageVersion.ToString();
                }
                NuGetManagedTemplatesSource source = new NuGetManagedTemplatesSource(_environmentSettings, this, packageLocation, sourceDetails);
                return InstallResult.CreateSuccess(source);
            }
            catch (DownloadException e)
            {
                return InstallResult.CreateFailure(InstallerErrorCode.DownloadFailed, e.Message);
            }
            catch (PackageNotFoundException e)
            {
                return InstallResult.CreateFailure(InstallerErrorCode.PackageNotFound, e.Message);
            }
            catch (InvalidNuGetSourceException e)
            {
                return InstallResult.CreateFailure(InstallerErrorCode.InvalidSource, e.Message);
            }
            catch (Exception e)
            {
                return InstallResult.CreateFailure(InstallerErrorCode.GenericError, $"Failed to install the package {installRequest.Identifier}, reason: {e.Message}");
            }
        }

        public TemplatesSourceData Serialize(IManagedTemplatesSource managedSource)
        {
            _ = managedSource ?? throw new ArgumentNullException(nameof(managedSource));
            if (!(managedSource is NuGetManagedTemplatesSource nuGetTemplatesSource))
            {
                return new TemplatesSourceData()
                {
                    InstallerId = FactoryId,
                    MountPointUri = managedSource.MountPointUri,
                    LastChangeTime = default
                };
            }

            return new TemplatesSourceData()
            {
                InstallerId = FactoryId,
                MountPointUri = nuGetTemplatesSource.MountPointUri,
                LastChangeTime = nuGetTemplatesSource.LastChangeTime,
                Details = nuGetTemplatesSource.Details
            };
        }

        public Task<UninstallResult> UninstallAsync(IManagedTemplatesSource managedSource)
        {
            _ = managedSource ?? throw new ArgumentNullException(nameof(managedSource));
            if (!(managedSource is NuGetManagedTemplatesSource))
            {
                return Task.FromResult(UninstallResult.CreateFailure(InstallerErrorCode.UnsupportedRequest, $"{managedSource.Identifier} is not supported by {Name}"));
            }
            try
            {
                _environmentSettings.Host.FileSystem.FileDelete(managedSource.MountPointUri);
                return Task.FromResult(UninstallResult.CreateSuccess());
            }
            catch (Exception ex)
            {
                return Task.FromResult(UninstallResult.CreateFailure(InstallerErrorCode.GenericError, $"Failed to uninstall {managedSource.Identifier}, reason: {ex.Message}"));
            }
        }

        private bool IsLocalPackage(InstallRequest installRequest)
        {
            return File.Exists(installRequest.Identifier);
        }
    }
}
