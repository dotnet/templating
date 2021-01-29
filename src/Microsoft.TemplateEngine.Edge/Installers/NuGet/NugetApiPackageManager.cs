using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SemanticVersion = Microsoft.TemplateEngine.Abstractions.SemanticVersion;
using NuGetSemanticVersion = NuGet.Versioning.SemanticVersion;
using System;
using System.Collections.Concurrent;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class NuGetApiPackageManager : IDownloader, IUpdateChecker
    {
        private readonly ILogger _nugetLogger = NullLogger.Instance;
        internal const string PublicNuGetFeed = "https://api.nuget.org/v3/index.json";
        private readonly IEngineEnvironmentSettings _environmentSettings;

        internal NuGetApiPackageManager(IEngineEnvironmentSettings settings)
        {
            _environmentSettings = settings;
        }

        public bool CanCheckForUpdate(NuGetManagedTemplatesSource source)
        {
            return true;
        }

        public bool CanDownloadPackage(InstallRequest installRequest)
        {
            return true;
        }

        public async Task<DownloadResult> DownloadPackageAsync(InstallRequest installRequest, string downloadPath)
        {
            IEnumerable<string> sources = null;
            if (installRequest.Details.ContainsKey(InstallRequest.NuGetSourcesKey))
            {
                sources = installRequest.Details[InstallRequest.NuGetSourcesKey].Split(InstallRequest.NuGetSourcesSeparator);
            }

            NuGetVersion packageVersion;
            string source;
            IPackageSearchMetadata packageMetadata;

            if (installRequest.Version is null)
            {
                (source, packageMetadata) = await GetLatestVersionInternalAsync(installRequest.Identifier, sources).ConfigureAwait(false);
            }
            else
            {
                packageVersion = new NuGetVersion(installRequest.Version.ToString());
                (source, packageMetadata) = await GetPackageMetadataAsync(installRequest.Identifier, packageVersion, sources).ConfigureAwait(false);
            }

            FindPackageByIdResource resource;
            SourceRepository repository = Repository.Factory.GetCoreV3(source);
            try
            {
                resource = await repository.GetResourceAsync<FindPackageByIdResource>();
            }
            catch (Exception e)
            {
                _nugetLogger.LogError($"Failed to load the NuGet source {source}");
                _nugetLogger.LogDebug($"Reason: {e.Message}");
                throw new InvalidNuGetSourceException("Failed to load NuGet source", new[] { source }, e);
            } 
 

            CancellationToken cancellationToken = CancellationToken.None;
            SourceCacheContext cache = new SourceCacheContext();
            string filePath = Path.Combine(downloadPath, packageMetadata.Identity.Id + "." + packageMetadata.Identity.Version + ".nupkg");
            try
            {
                using Stream packageStream = _environmentSettings.Host.FileSystem.CreateFile(filePath);
                if (await resource.CopyNupkgToStreamAsync(
                    packageMetadata.Identity.Id,
                    packageMetadata.Identity.Version,
                    packageStream,
                    cache,
                    _nugetLogger,
                    cancellationToken).ConfigureAwait(false))
                {
                    return new DownloadResult
                    {
                        NuGetSource = source,
                        FullPath = filePath,
                        PackageIdentifier = packageMetadata.Identity.Id,
                        PackageVersion = packageMetadata.Identity.Version.ToSemanticVersion(),
                        Author = packageMetadata.Authors
                    };
                }
                else
                {
                    _nugetLogger.LogWarning($"Failed to download {packageMetadata.Identity.Id}::{packageMetadata.Identity.Version} from NuGet feed {source}");
                    throw new DownloadException(packageMetadata.Identity.Id, packageMetadata.Identity.Version, new[] { source });
                }
            }
            catch (Exception e)
            {
                _nugetLogger.LogWarning($"Failed to download {packageMetadata.Identity.Id}::{packageMetadata.Identity.Version} from NuGet feed {source}");
                _nugetLogger.LogDebug($"Reason: {e.Message}");
                throw new DownloadException(packageMetadata.Identity.Id, packageMetadata.Identity.Version, new[] { source }, e.InnerException);
            }
        }

        public async Task<SemanticVersion> GetLatestVersionAsync(NuGetManagedTemplatesSource source)
        {
            return await GetLatestVersionAsync(source.Identifier).ConfigureAwait(false);
        }

        private async Task<SemanticVersion> GetLatestVersionAsync(string packageIdentifier)
        {
            var (_, package) = await GetLatestVersionInternalAsync(packageIdentifier).ConfigureAwait(false);
            return package.Identity.Version.ToSemanticVersion();
        }

        private async Task<(string, IPackageSearchMetadata)> GetLatestVersionInternalAsync(string packageIdentifier, IEnumerable<string> sources = null)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            SourceCacheContext cache = new SourceCacheContext();

            ConcurrentDictionary<string, PackageMetadataResource> resources = new ConcurrentDictionary<string, PackageMetadataResource>();
            IEnumerable<string> attemptedSources = sources != null ? sources : new[] { PublicNuGetFeed };
            foreach (string source in attemptedSources)
            {
                SourceRepository repository = Repository.Factory.GetCoreV3(source);
                try
                {
                    PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>();
                    resources[source] = resource;
                }
                catch (Exception e)
                {
                    _nugetLogger.LogError($"Failed to load the NuGet source {source}");
                    _nugetLogger.LogDebug($"Reason: {e.Message}");
                }
            }
        
            if (!resources.Any())
            {
                throw new InvalidNuGetSourceException("Failed to load NuGet sources", attemptedSources);
            }

            ConcurrentDictionary<string, IEnumerable<IPackageSearchMetadata>> searchResults = new ConcurrentDictionary<string, IEnumerable<IPackageSearchMetadata>>();
            foreach (KeyValuePair<string, PackageMetadataResource> resource in resources)
            {   
                IEnumerable<IPackageSearchMetadata> foundPackages = await resource.Value.GetMetadataAsync(
                    packageIdentifier,
                    includePrerelease: false,
                    includeUnlisted: false,
                    cache,
                    _nugetLogger,
                    cancellationToken).ConfigureAwait(false);
                if (foundPackages.Any())
                {
                    _nugetLogger.LogVerbose($"Found {foundPackages.Count()} {packageIdentifier} packages in NuGet feed {resource.Key}");
                    searchResults[resource.Key] = foundPackages;
                }
                else
                {
                    _nugetLogger.LogWarning($"{packageIdentifier} is not found in NuGet feed {resource.Key}");
                }
            }

            var accumulativeSearchResults = searchResults
                .SelectMany(result => result.Value.Select(p => new { source = result.Key, package = p}));

            if (!accumulativeSearchResults.Any())
            {
                throw new PackageNotFoundException(packageIdentifier, attemptedSources);
            }

            var latestVersion = accumulativeSearchResults.Aggregate(
                (current, max) =>
                {
                    if (max == null) return current;
                    return current.package.Identity.Version > max.package.Identity.Version ? current : max;
                });
            return (latestVersion.source, latestVersion.package);
        }

        private async Task<(string, IPackageSearchMetadata)> GetPackageMetadataAsync(string packageIdentifier, NuGetVersion packageVersion, IEnumerable<string> sources = null)
        {
            CancellationToken cancellationToken = CancellationToken.None;
            SourceCacheContext cache = new SourceCacheContext();

            ConcurrentDictionary<string, PackageMetadataResource> resources = new ConcurrentDictionary<string, PackageMetadataResource>();
            IEnumerable<string> attemptedSources = sources != null ? sources : new[] { PublicNuGetFeed };
            foreach (string source in attemptedSources)
            {
                SourceRepository repository = Repository.Factory.GetCoreV3(source);
                try
                {
                    PackageMetadataResource resource = await repository.GetResourceAsync<PackageMetadataResource>();
                    resources[source] = resource;
                }
                catch (Exception e)
                {
                    _nugetLogger.LogError($"Failed to load the NuGet source {source}");
                    _nugetLogger.LogDebug($"Reason: {e.Message}");
                }
            }

            if (!resources.Any())
            {
                throw new InvalidNuGetSourceException("Failed to load NuGet sources", attemptedSources);
            }

            ConcurrentDictionary<string, IEnumerable<IPackageSearchMetadata>> searchResults = new ConcurrentDictionary<string, IEnumerable<IPackageSearchMetadata>>();
            foreach (KeyValuePair<string, PackageMetadataResource> resource in resources)
            {
                IEnumerable<IPackageSearchMetadata> foundPackages = await resource.Value.GetMetadataAsync(
                    packageIdentifier,
                    includePrerelease: false,
                    includeUnlisted: false,
                    cache,
                    _nugetLogger,
                    cancellationToken).ConfigureAwait(false);

                var matchedVersion = foundPackages.FirstOrDefault(package => package.Identity.Version == packageVersion);

                if (matchedVersion != null)
                {
                    return (resource.Key, matchedVersion);
                }
                else
                {
                    _nugetLogger.LogWarning($"{packageIdentifier}::{packageVersion} is not found in NuGet feed {resource.Key}");
                }
            }

            throw new PackageNotFoundException(packageIdentifier, packageVersion, attemptedSources);
        }

    }
}
