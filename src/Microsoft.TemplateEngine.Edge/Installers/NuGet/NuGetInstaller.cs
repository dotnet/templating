// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.GlobalSettings;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using NuGet.Packaging;
using NuGet.Versioning;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class NuGetInstaller : IInstaller
    {
        private const string DebugLogCategory = "Installer";
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
            try
            {
                ReadPackageInformation(installationRequest.Identifier);
            }
            catch (Exception)
            {
                _environmentSettings.Host.LogDiagnosticMessage($"{installationRequest.Identifier} is not a local NuGet package.", DebugLogCategory);

                //check if identifier is a valid package ID
                bool validPackageId = PackageIdValidator.IsValidPackageId(installationRequest.Identifier);
                //check if version is specified it is correct version
                bool hasValidVersion = string.IsNullOrWhiteSpace(installationRequest.Version) || NuGetVersion.TryParse(installationRequest.Version, out _);
                if (!validPackageId)
                {
                    _environmentSettings.Host.LogDiagnosticMessage($"{installationRequest.Identifier} is not a valid NuGet package ID.", DebugLogCategory);
                }
                if (!hasValidVersion)
                {
                    _environmentSettings.Host.LogDiagnosticMessage($"{installationRequest.Version} is not a valid NuGet package version.", DebugLogCategory);
                }
                if (validPackageId && hasValidVersion)
                {
                    _environmentSettings.Host.LogDiagnosticMessage($"{installationRequest.Version} is identified as the downloadable NuGet package.", DebugLogCategory);
                }

                //not a local package file
                return Task.FromResult(validPackageId && hasValidVersion);
            }
            _environmentSettings.Host.LogDiagnosticMessage($"{installationRequest.Identifier} is identified as the local NuGet package.", DebugLogCategory);
            return Task.FromResult(true);
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
                            return _updateChecker.GetLatestVersionAsync(nugetSource, CancellationToken.None);
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
                            _environmentSettings.Host.LogDiagnosticMessage($"Retreving latest version for package {source.DisplayName} failed.", DebugLogCategory);
                            _environmentSettings.Host.LogDiagnosticMessage($"Details:{e.ToString()}", DebugLogCategory);
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
                    nuGetPackageInfo = await _packageDownloader.DownloadPackageAsync(installRequest, _installPath, CancellationToken.None).ConfigureAwait(false);
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
                _environmentSettings.Host.LogDiagnosticMessage($"Installing {installRequest.DisplayName} failed.", DebugLogCategory);
                _environmentSettings.Host.LogDiagnosticMessage($"Details:{e.ToString()}", DebugLogCategory);
                return InstallResult.CreateFailure(installRequest, InstallerErrorCode.GenericError, $"Failed to install the package {installRequest.DisplayName}, reason: {e.Message}");
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
                _environmentSettings.Host.LogDiagnosticMessage($"Uninstalling {managedSource.DisplayName} failed.", DebugLogCategory);
                _environmentSettings.Host.LogDiagnosticMessage($"Details:{ex.ToString()}", DebugLogCategory);
                return Task.FromResult(UninstallResult.CreateFailure(managedSource, InstallerErrorCode.GenericError, $"Failed to uninstall {managedSource.DisplayName}, reason: {ex.Message}"));
            }
        }

        public async Task<UpdateResult> UpdateAsync(UpdateRequest updateRequest)
        {
            _ = updateRequest ?? throw new ArgumentNullException(nameof(updateRequest));
            if (string.IsNullOrWhiteSpace(updateRequest.Version))
            {
                throw new ArgumentException("Version cannot be null or empty", nameof(updateRequest.Version));
            }

            InstallRequest installRequest = new InstallRequest
            {
                Identifier = updateRequest.Source.Identifier,
                Version = updateRequest.Version
            };

            var nuGetManagedSource = updateRequest.Source as NuGetManagedTemplatesSource;
            if (nuGetManagedSource != null && !string.IsNullOrWhiteSpace(nuGetManagedSource.NuGetSource))
            {
                installRequest.Details = new Dictionary<string, string>()
                {
                    { InstallerConstants.NuGetSourcesKey, nuGetManagedSource.NuGetSource }
                };
            }
            return UpdateResult.FromInstallResult(updateRequest, await InstallAsync(installRequest).ConfigureAwait(false));
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
                _environmentSettings.Host.LogDiagnosticMessage(DebugLogCategory, $"Details: {ex.ToString()}.");
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
                _environmentSettings.Host.LogDiagnosticMessage(DebugLogCategory, $"Details: {ex.ToString()}.");
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
