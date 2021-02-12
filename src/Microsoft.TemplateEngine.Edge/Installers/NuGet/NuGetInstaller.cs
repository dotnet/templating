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
using NuGet.Packaging;

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
                Dictionary<string, string> sourceDetails = new Dictionary<string, string>();
                NuGetPackageInfo nuGetPackageInfo;
                if (IsLocalPackage(installRequest))
                {
                    sourceDetails[NuGetManagedTemplatesSource.LocalPackageKey] = true.ToString();
                    nuGetPackageInfo = InstallLocalPackage(installRequest);
                }
                else
                {
                    nuGetPackageInfo = await _packageDownloader.DownloadPackageAsync(installRequest, _installPath).ConfigureAwait(false);
                }

                sourceDetails[NuGetManagedTemplatesSource.AuthorKey] = nuGetPackageInfo.Author;
                sourceDetails[NuGetManagedTemplatesSource.NuGetSourceKey] = nuGetPackageInfo.NuGetSource;
                sourceDetails[NuGetManagedTemplatesSource.PackageIdKey] = nuGetPackageInfo.PackageIdentifier;
                sourceDetails[NuGetManagedTemplatesSource.PackageVersionKey] = nuGetPackageInfo.PackageVersion.ToString();
                NuGetManagedTemplatesSource source = new NuGetManagedTemplatesSource(_environmentSettings, this, nuGetPackageInfo.FullPath, sourceDetails);
                return InstallResult.CreateSuccess(installRequest, source);
            }
            catch (DownloadException e)
            {
                return InstallResult.CreateFailure(installRequest, InstallerErrorCode.DownloadFailed, e.Message);
            }
            catch (PackageNotFoundException e)
            {
                return InstallResult.CreateFailure(installRequest, InstallerErrorCode.PackageNotFound, e.Message);
            }
            catch (InvalidNuGetSourceException e)
            {
                return InstallResult.CreateFailure(installRequest, InstallerErrorCode.InvalidSource, e.Message);
            }
            catch (InvalidNuGetPackageException e)
            {
                return InstallResult.CreateFailure(installRequest, InstallerErrorCode.InvalidPackage, e.Message);
            }
            catch (Exception e)
            {
                return InstallResult.CreateFailure(installRequest, InstallerErrorCode.GenericError, $"Failed to install the package {installRequest.Identifier}, reason: {e.Message}");
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
                return Task.FromResult(UninstallResult.CreateFailure(managedSource, InstallerErrorCode.UnsupportedRequest, $"{managedSource.Identifier} is not supported by {Name}"));
            }
            try
            {
                _environmentSettings.Host.FileSystem.FileDelete(managedSource.MountPointUri);
                return Task.FromResult(UninstallResult.CreateSuccess(managedSource));
            }
            catch (Exception ex)
            {
                return Task.FromResult(UninstallResult.CreateFailure(managedSource, InstallerErrorCode.GenericError, $"Failed to uninstall {managedSource.Identifier}, reason: {ex.Message}"));
            }
        }

        private bool IsLocalPackage(InstallRequest installRequest)
        {
            return _environmentSettings.Host.FileSystem.FileExists(installRequest.Identifier);
        }

        private NuGetPackageInfo InstallLocalPackage(InstallRequest installRequest)
        {
            _ = installRequest ?? throw new ArgumentNullException(nameof(installRequest));

            NuGetPackageInfo packageInfo;
            try
            {
                packageInfo = ReadPackageInformation(installRequest.Identifier);
            }
            catch (Exception ex)
            {
                _environmentSettings.Host.OnCriticalError(null, $"Failed to read content of package {installRequest.Identifier}.", null, 0);
                _environmentSettings.Host.LogDiagnosticMessage("Installer", $"Reason: {ex.Message}.");
                throw new InvalidNuGetPackageException(installRequest.Identifier, ex);
            }
            string targetPackageLocation = Path.Combine(_installPath, packageInfo.PackageIdentifier + "." + packageInfo.PackageVersion + ".nupkg");
            if (_environmentSettings.Host.FileSystem.FileExists(targetPackageLocation))
            {
                _environmentSettings.Host.OnCriticalError(null, $"File {targetPackageLocation} already exists.", null, 0);
                throw new DownloadException(packageInfo.PackageIdentifier, packageInfo.PackageVersion, installRequest.Identifier);
            }

            try
            {
                _environmentSettings.Host.FileSystem.FileCopy(installRequest.Identifier, targetPackageLocation, overwrite: false);
            }
            catch (Exception ex)
            {
                _environmentSettings.Host.OnCriticalError(null, $"Failed to copy package {installRequest.Identifier} to {targetPackageLocation}.", null, 0);
                _environmentSettings.Host.LogDiagnosticMessage("Installer", $"Reason: {ex.Message}.");
                throw new DownloadException(packageInfo.PackageIdentifier, packageInfo.PackageVersion, installRequest.Identifier);
            }
            return packageInfo;
        }

        private NuGetPackageInfo ReadPackageInformation(string packageLocation)
        {
            using Stream inputStream = _environmentSettings.Host.FileSystem.OpenRead(packageLocation);
            using PackageArchiveReader reader = new PackageArchiveReader(inputStream);

            NuspecReader nuspec = reader.NuspecReader;

            return new NuGetPackageInfo
            {
                FullPath = packageLocation,
                Author = nuspec.GetAuthors(),
                PackageIdentifier = nuspec.GetId(),
                PackageVersion = nuspec.GetVersion().ToNormalizedString()
            };
        }
    }
}
