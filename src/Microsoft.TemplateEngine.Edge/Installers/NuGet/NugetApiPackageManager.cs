// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class NuGetApiPackageManager : IDownloader, IUpdateChecker
    {
        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly ILogger _nugetLogger;
        private readonly SourceCacheContext _cacheSettings = new SourceCacheContext()
        {
            NoCache = true,
            DirectDownload = true
        };

        internal NuGetApiPackageManager(IEngineEnvironmentSettings settings)
        {
            _environmentSettings = settings;
            _nugetLogger = new NuGetLogger(settings);
        }

        /// <summary>
        /// Downloads the package from configured NuGet package feeds. NuGet feeds to use are read for current directory, if additional feeds are specified in installation request, they are checked as well.
        /// </summary>
        /// <param name="installRequest"><see cref="InstallRequest"/> that defines the package to download</param>
        /// <param name="downloadPath">path to download to</param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="NuGetPackageInfo"/>containing full path to downloaded package and package details</returns>
        /// <exception cref="InvalidNuGetSourceException">when sources passed to install request are not valid NuGet sources or failed to read default NuGet configuration</exception>
        /// <exception cref="DownloadException">when the download of the package failed</exception>
        /// <exception cref="PackageNotFoundException">when the package cannot be find in default or passed to install request NuGet feeds</exception>
        public async Task<NuGetPackageInfo> DownloadPackageAsync(InstallRequest installRequest, string downloadPath, CancellationToken cancellationToken)
        {
            string[] sources = Array.Empty<string>();
            if (installRequest.Details?.ContainsKey(InstallerConstants.NuGetSourcesKey) ?? false)
            {
                sources = installRequest.Details[InstallerConstants.NuGetSourcesKey].Split(InstallerConstants.NuGetSourcesSeparator);
            }

            IEnumerable<PackageSource> packagesSources = LoadNuGetSources(sources);

            NuGetVersion packageVersion;
            PackageSource source;
            IPackageSearchMetadata packageMetadata;

            if (string.IsNullOrWhiteSpace(installRequest.Version))
            {
                (source, packageMetadata) = await GetLatestVersionInternalAsync(installRequest.Identifier, packagesSources, includePreview: false, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                packageVersion = new NuGetVersion(installRequest.Version.ToString());
                (source, packageMetadata) = await GetPackageMetadataAsync(installRequest.Identifier, packageVersion, packagesSources, cancellationToken).ConfigureAwait(false);
            }

            FindPackageByIdResource resource;
            SourceRepository repository = Repository.Factory.GetCoreV3(source);
            try
            {
                resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _nugetLogger.LogError($"Failed to load the NuGet source {source.Source}.");
                _nugetLogger.LogDebug($"Details: {e.ToString()}.");
                throw new InvalidNuGetSourceException("Failed to load NuGet source", new[] { source.Source }, e);
            }

            string filePath = Path.Combine(downloadPath, packageMetadata.Identity.Id + "." + packageMetadata.Identity.Version + ".nupkg");
            if (_environmentSettings.Host.FileSystem.FileExists(filePath))
            {
                _nugetLogger.LogError($"File {filePath} already exists.");
                throw new DownloadException(packageMetadata.Identity.Id, packageMetadata.Identity.Version.ToNormalizedString(), new[] { source.Source });
            }
            try
            {
                using Stream packageStream = _environmentSettings.Host.FileSystem.CreateFile(filePath);
                if (await resource.CopyNupkgToStreamAsync(
                    packageMetadata.Identity.Id,
                    packageMetadata.Identity.Version,
                    packageStream,
                    _cacheSettings,
                    _nugetLogger,
                    cancellationToken).ConfigureAwait(false))
                {
                    return new NuGetPackageInfo
                    {
                        NuGetSource = source.Source,
                        FullPath = filePath,
                        PackageIdentifier = packageMetadata.Identity.Id,
                        PackageVersion = packageMetadata.Identity.Version.ToNormalizedString(),
                        Author = packageMetadata.Authors
                    };
                }
                else
                {
                    _nugetLogger.LogWarning($"Failed to download {packageMetadata.Identity.Id}::{packageMetadata.Identity.Version} from NuGet feed {source.Source}");
                    try
                    {
                        _environmentSettings.Host.FileSystem.FileDelete(filePath);
                    }
                    catch (Exception ex)
                    {
                        _nugetLogger.LogWarning($"Failed to remove {filePath} after failed download. Remove the file manually if it exists.");
                        _nugetLogger.LogDebug($"Details: {ex.ToString()}.");
                    }
                    throw new DownloadException(packageMetadata.Identity.Id, packageMetadata.Identity.Version.ToNormalizedString(), new[] { source.Source });
                }
            }
            catch (Exception e)
            {
                _nugetLogger.LogWarning($"Failed to download {packageMetadata.Identity.Id}::{packageMetadata.Identity.Version} from NuGet feed {source.Source}.");
                _nugetLogger.LogDebug($"Details: {e.ToString()}.");
                try
                {
                    _environmentSettings.Host.FileSystem.FileDelete(filePath);
                }
                catch (Exception ex)
                {
                    _nugetLogger.LogWarning($"Failed to remove {filePath} after failed download. Remove the file manually if it exists.");
                    _nugetLogger.LogDebug($"Details: {ex.ToString()}.");
                }
                throw new DownloadException(packageMetadata.Identity.Id, packageMetadata.Identity.Version.ToNormalizedString(), new[] { source.Source }, e.InnerException);
            }
        }

        /// <summary>
        /// Gets the latest stable version for the package. If the package has preview version installed, returns the latest preview.
        /// Uses NuGet feeds configured for current directory and the source if specified from <paramref name="source"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="CheckUpdateResult"/> containing the latest version for the <paramref name="source"/>.</returns>
        /// <exception cref="InvalidNuGetSourceException">when sources passed to install request are not valid NuGet feeds or failed to read default NuGet configuration</exception>
        /// <exception cref="PackageNotFoundException">when the package cannot be find in default or source NuGet feeds</exception>
        public async Task<CheckUpdateResult> GetLatestVersionAsync(NuGetManagedTemplatesSource source, CancellationToken cancellationToken)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));

            //if preview version is installed, check for the latest preview version, otherwise for latest stable
            bool previewVersionInstalled = false;
            if (NuGetVersion.TryParse(source.Version, out NuGetVersion currentVersion))
            {
                previewVersionInstalled = currentVersion.IsPrerelease;
            }

            IEnumerable<PackageSource> packageSources = LoadNuGetSources(source.NuGetSource);
            var (_, package) = await GetLatestVersionInternalAsync(source.Identifier, packageSources, previewVersionInstalled, cancellationToken).ConfigureAwait(false);
            bool isLatestVersion = currentVersion != null ? currentVersion >= package.Identity.Version : false;
            return CheckUpdateResult.CreateSuccess(source, package.Identity.Version.ToNormalizedString(), isLatestVersion);
        }

        private async Task<(PackageSource, IPackageSearchMetadata)> GetLatestVersionInternalAsync(string packageIdentifier, IEnumerable<PackageSource> packageSources, bool includePreview, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(packageIdentifier))
            {
                throw new ArgumentException($"{nameof(packageIdentifier)} cannot be null or empty", nameof(packageIdentifier));
            }
            _ = packageSources ?? throw new ArgumentNullException(nameof(packageSources));


            ConcurrentDictionary<PackageSource, IEnumerable<IPackageSearchMetadata>> searchResults = new ConcurrentDictionary<PackageSource, IEnumerable<IPackageSearchMetadata>>();
            await Task.WhenAll(packageSources.Select(async source =>
                {
                    IEnumerable<IPackageSearchMetadata> foundPackages = await GetPackageMetadataAsync(source, packageIdentifier, includePrerelease: true, cancellationToken).ConfigureAwait(false);
                    if (foundPackages == null)
                    {
                        return;
                    }
                    searchResults[source] = foundPackages;
                })).ConfigureAwait(false);

            if (!searchResults.Any())
            {
                throw new InvalidNuGetSourceException("Failed to load NuGet sources", packageSources.Select(source => source.Source));
            }

            var accumulativeSearchResults = searchResults
                .SelectMany(result => result.Value.Select(p => new { source = result.Key, package = p }));

            if (!accumulativeSearchResults.Any())
            {
                _nugetLogger.LogWarning($"{packageIdentifier} is not found in NuGet feeds {string.Join(", ", packageSources.Select(source => source.Source))}.");
                throw new PackageNotFoundException(packageIdentifier, packageSources.Select(source => source.Source));
            }

            var latestVersion = accumulativeSearchResults.Aggregate(
                (max, current) =>
                {
                    if (max == null) return current;
                    return current.package.Identity.Version > max.package.Identity.Version ? current : max;
                });

            var latestStableVersion = accumulativeSearchResults.Aggregate(
                (max, current) =>
                {
                    if (current.package.Identity.Version.IsPrerelease) return max;
                    if (max == null) return current;
                    return current.package.Identity.Version > max.package.Identity.Version ? current : max;
                });

            if (latestStableVersion != null && !includePreview)
            {
                return (latestStableVersion.source, latestStableVersion.package);
            }
            else
            {
                return (latestVersion.source, latestVersion.package);
            }
        }

        private async Task<(PackageSource, IPackageSearchMetadata)> GetPackageMetadataAsync(string packageIdentifier, NuGetVersion packageVersion, IEnumerable<PackageSource> sources, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(packageIdentifier))
            {
                throw new ArgumentException($"{nameof(packageIdentifier)} cannot be null or empty", nameof(packageIdentifier));
            }
            _ = packageVersion ?? throw new ArgumentNullException(nameof(packageVersion));
            _ = sources ?? throw new ArgumentNullException(nameof(sources));

            bool atLeastOneSourceValid = false;
            foreach (PackageSource source in sources)
            {
                _nugetLogger.LogDebug($"Searching {packageIdentifier}::{packageVersion} in {source.Source}.");
                IEnumerable<IPackageSearchMetadata> foundPackages = await GetPackageMetadataAsync(source, packageIdentifier, includePrerelease:true, cancellationToken).ConfigureAwait(false);
                if (foundPackages == null)
                {
                    continue;
                }
                atLeastOneSourceValid = true;
                IPackageSearchMetadata matchedVersion = foundPackages.FirstOrDefault(package => package.Identity.Version == packageVersion);
                if (matchedVersion != null)
                {
                    _nugetLogger.LogDebug($"{packageIdentifier}::{packageVersion} was found in {source.Source}.");
                    return (source, matchedVersion);
                }
                else
                {
                    _nugetLogger.LogDebug($"{packageIdentifier}::{packageVersion} is not found in NuGet feed {source.Source}.");
                }
            }

            if (!atLeastOneSourceValid)
            {
                throw new InvalidNuGetSourceException("Failed to load NuGet sources", sources.Select(s => s.Source));
            }

            _nugetLogger.LogWarning($"{packageIdentifier}::{packageVersion} is not found in NuGet feeds {string.Join(", ", sources.Select(source => source.Source))}.");
            throw new PackageNotFoundException(packageIdentifier, packageVersion, sources.Select(source => source.Source));
        }

        private async Task<IEnumerable<IPackageSearchMetadata>> GetPackageMetadataAsync(PackageSource source, string packageIdentifier, bool includePrerelease = false, CancellationToken cancellationToken = default)
        {
            _nugetLogger.LogDebug($"Searching for {packageIdentifier} in {source.Source}.");
            try
            {
                SourceRepository repository = Repository.Factory.GetCoreV3(source);
                PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>(cancellationToken).ConfigureAwait(false);
                IEnumerable<IPackageSearchMetadata> foundPackages = await resource.GetMetadataAsync(
                    packageIdentifier,
                    includePrerelease: includePrerelease,
                    includeUnlisted: false,
                    _cacheSettings,
                    _nugetLogger,
                    cancellationToken).ConfigureAwait(false);

                if (foundPackages.Any())
                {
                    _nugetLogger.LogDebug($"Found {foundPackages.Count()} versions for {packageIdentifier} in NuGet feed {source.Source}.");
                }
                else
                {
                    _nugetLogger.LogDebug($"{packageIdentifier} is not found in NuGet feed {source.Source}.");
                }
                return foundPackages;
            }
            catch (Exception ex)
            {
                _nugetLogger.LogError($"Failed to read package information from NuGet source {source.Source}.");
                _nugetLogger.LogDebug($"Details: {ex.ToString()}.");
            }
            return null;
        }

        private IEnumerable<PackageSource> LoadNuGetSources(params string[] additionalSources)
        {
            IEnumerable<PackageSource> defaultSources;
            string currentDirectory = string.Empty;
            try
            {
                currentDirectory = Directory.GetCurrentDirectory();
                ISettings settings = global::NuGet.Configuration.Settings.LoadDefaultSettings(currentDirectory);
                PackageSourceProvider packageSourceProvider = new PackageSourceProvider(settings);
                defaultSources = packageSourceProvider.LoadPackageSources().Where(source => source.IsEnabled);
            }
            catch (Exception ex)
            {
                _nugetLogger.LogError($"Failed to load NuGet sources configured for the folder {currentDirectory}.");
                _nugetLogger.LogDebug($"Details: {ex.ToString()}.");
                throw new InvalidNuGetSourceException($"Failed to load NuGet sources configured for the folder {currentDirectory}", ex);
            }

            if (!additionalSources?.Any() ?? true)
            {
                if (!defaultSources.Any())
                {
                    _nugetLogger.LogError($"No NuGet sources are defined or enabled.");
                    throw new InvalidNuGetSourceException("No NuGet sources are defined or enabled");
                }
                return defaultSources;
            }

            List<PackageSource> customSources = new List<PackageSource>();
            foreach (string source in additionalSources)
            {
                if (string.IsNullOrWhiteSpace(source))
                {
                    continue;
                }
                if (defaultSources.Any(s => s.Source.Equals(source, StringComparison.OrdinalIgnoreCase)))
                {
                    _nugetLogger.LogDebug($"Custom source {source} is already loaded from default configuration.");
                    continue;
                }
                PackageSource packageSource = new PackageSource(source);
                if (packageSource.TrySourceAsUri == null)
                {
                    _nugetLogger.LogWarning($"Failed to load NuGet source {source}: the source is not valid. It will be skipped in further processing.");
                    continue;
                }
                customSources.Add(packageSource);
            }

            IEnumerable<PackageSource> retrievedSources = defaultSources.Concat(customSources);
            if (!retrievedSources.Any())
            {
                _nugetLogger.LogError($"No NuGet sources are defined or enabled.");
                throw new InvalidNuGetSourceException("No NuGet sources are defined or enabled");
            }
            return retrievedSources;
        }
    }
}
