// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateSearch.Common.Abstractions;
using Microsoft.TemplateSearch.TemplateDiscovery.PackChecking;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Catalog;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Microsoft.TemplateSearch.TemplateDiscovery.NuGet
{
    internal class TemplatePackageLeafProcessor(Func<PackageDetailsCatalogLeaf, bool> filter) : ICatalogLeafProcessor
    {
        private readonly Dictionary<string, string> _packageVersions = new Dictionary<string, string>();

        public List<KeyValuePair<string, string>> Packages => _packageVersions.ToList();

        public int PackageCount => _packageVersions.Count;

        public Task<bool> ProcessPackageDeleteAsync(PackageDeleteCatalogLeaf leaf)
        {
            if (_packageVersions.TryGetValue(leaf.PackageId, out var _))
            {
                lock (_packageVersions)
                {
                    _packageVersions.Remove(leaf.PackageId);
                }
            }
            return Task.FromResult(true);
        }

        public Task<bool> ProcessPackageDetailsAsync(PackageDetailsCatalogLeaf leaf)
        {
            if (filter.Invoke(leaf))
            {
                ExtractNugetPackageInfo(leaf);
                return Task.FromResult(true);
            }
            return Task.FromResult(true);
        }

        private void ExtractNugetPackageInfo(PackageDetailsCatalogLeaf leaf)
        {
            lock (_packageVersions)
            {
                _packageVersions[leaf.PackageId] = leaf.PackageVersion;
            }
        }
    }

    internal class MemoryCursor(DateTimeOffset initialCursor) : ICursor
    {
        public DateTimeOffset? Cursor { get; set; } = initialCursor;

        public Task<DateTimeOffset?> GetAsync()
        {
            return Task.FromResult(Cursor);
        }

        public Task SetAsync(DateTimeOffset value)
        {
            Cursor = value;
            return Task.CompletedTask;
        }
    }

    internal class NuGetPackProvider : IPackProvider, IDisposable
    {
        private const string NuGetOrgFeed = "https://api.nuget.org/v3/index.json";
        private const string DownloadPackageFileNameFormat = "{0}.{1}.nupkg";
        private const string DownloadedPacksDir = "DownloadedPacks";
        private readonly string _packageTempPath;
        private readonly SourceRepository _repository;
        private readonly SourceCacheContext _cacheContext = new SourceCacheContext();
        private readonly FindPackageByIdResource _downloadResource;
        private readonly Uri _searchUrl;
        private readonly bool _includePreview;
        private readonly Func<PackageDetailsCatalogLeaf, bool> _filter;
        private readonly TemplatePackageLeafProcessor _processor;
        private readonly PackageSearchResource _searchResource;
        private readonly CatalogProcessor _catalogProcessor;
        private readonly HttpClient _httpClient;

        internal NuGetPackProvider(string name, DateTimeOffset previousCache, Func<PackageDetailsCatalogLeaf, bool> filter, DirectoryInfo packageTempBasePath, bool includePreviewPacks)
        {
            Name = name;
            _packageTempPath = Path.GetFullPath(Path.Combine(packageTempBasePath.FullName, DownloadedPacksDir, Name));
            _repository = Repository.Factory.GetCoreV3(NuGetOrgFeed);
            _downloadResource = _repository.GetResource<FindPackageByIdResource>();
            var index = _repository.GetResource<ServiceIndexResourceV3>();
            _searchUrl = index.GetServiceEntryUris(ServiceTypes.SearchQueryService)[0];
            _includePreview = includePreviewPacks;
            _filter = filter;

            DateTimeOffset cursor = previousCache;
            _processor = new TemplatePackageLeafProcessor(_filter);
            _searchResource = _repository.GetResource<PackageSearchResource>();
            CatalogProcessorSettings settings = new()
            {
                DefaultMinCommitTimestamp = cursor
            };

            var loggerFactory = new LoggerFactory();
            var cursor2 = new MemoryCursor(cursor);
            _httpClient = new HttpClient(new HttpClientHandler() { CheckCertificateRevocationList = true });
            var simpleHttpClient = new SimpleHttpClient(_httpClient, loggerFactory.CreateLogger<SimpleHttpClient>());
            var client = new CatalogClient(simpleHttpClient, loggerFactory.CreateLogger<CatalogClient>());
            _catalogProcessor = new CatalogProcessor(cursor2, client, _processor, settings, loggerFactory.CreateLogger<CatalogProcessor>());

            if (!Directory.Exists(_packageTempPath))
            {
                Directory.CreateDirectory(_packageTempPath);
            }
        }

        public string Name { get; private set; }

        public bool SupportsGetPackageInfoViaApi => true;

        public async IAsyncEnumerable<ITemplatePackageInfo> GetCandidatePacksAsync([EnumeratorCancellation] CancellationToken token)
        {
            await _catalogProcessor.ProcessAsync().ConfigureAwait(false);
            var count = 0;
            foreach ((var packageId, var packageVersion) in _processor.Packages)
            {
                count = count++;
                if (count % 100 == 0)
                {
                    Console.WriteLine($"Processed {count} packages");
                }
                var filters = new SearchFilter(packageVersion.Contains("-"))
                {
                    IncludeDelisted = true
                };
                var details = (await _searchResource.SearchAsync($"id:{packageId} version:{packageVersion}", filters, 0, 1, NullLogger.Instance, token).ConfigureAwait(false)).First();
                var templatePackage = new NuGetPackageSourceInfo(packageId, packageVersion)
                {
                    Description = details.Description,
                    IconUrl = details.IconUrl?.ToString(),
                    Owners = details.Owners.Split(";"),
                    TotalDownloads = details.DownloadCount ?? 0,
                    Reserved = details.PrefixReserved,
                };
                yield return templatePackage;
            }
        }

        public async Task<IDownloadedPackInfo> DownloadPackageAsync(ITemplatePackageInfo packinfo, CancellationToken token)
        {
            string packageFileName = string.Format(DownloadPackageFileNameFormat, packinfo.Name, packinfo.Version);
            string outputPackageFileNameFullPath = Path.Combine(_packageTempPath, packageFileName);

            try
            {
                if (File.Exists(outputPackageFileNameFullPath))
                {
                    return new DownloadedPackInfo(packinfo, outputPackageFileNameFullPath);
                }
                else
                {
                    using Stream packageStream = File.Create(outputPackageFileNameFullPath);
                    if (await _downloadResource.CopyNupkgToStreamAsync(
                        packinfo.Name,
                        new NuGetVersion(packinfo.Version!),
                        packageStream,
                        _cacheContext,
                        NullLogger.Instance,
                        token).ConfigureAwait(false))
                    {
                        return new DownloadedPackInfo(packinfo, outputPackageFileNameFullPath);
                    }
                    else
                    {
                        throw new Exception($"Download failed: {nameof(_downloadResource.CopyNupkgToStreamAsync)} returned false.");
                    }
                }
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to download package {packinfo.Name} {packinfo.Version}, reason: {e}.");
                throw;
            }
        }

        public Task<int> GetPackageCountAsync(CancellationToken token)
        {
            return Task.FromResult(_processor.PackageCount);
        }

        public async Task DeleteDownloadedPacksAsync()
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Directory.Delete(_packageTempPath, true);
                    return;
                }
                catch (IOException)
                {
                    Console.WriteLine($"Failed to remove {_packageTempPath}, retrying in 1 sec");
                }
                await Task.Delay(1000).ConfigureAwait(false);
            }
            Console.WriteLine($"Failed to remove {_packageTempPath}, remove it manually.");
        }

        public async Task<(ITemplatePackageInfo PackageInfo, bool Removed)> GetPackageInfoAsync(string packageIdentifier, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(packageIdentifier))
            {
                throw new ArgumentException($"{nameof(packageIdentifier)} cannot be null or empty", nameof(packageIdentifier));
            }

            try
            {
                PackageMetadataResource resource = await _repository.GetResourceAsync<PackageMetadataResource>(cancellationToken).ConfigureAwait(false);
                IEnumerable<IPackageSearchMetadata> foundPackages = await resource.GetMetadataAsync(
                    packageIdentifier,
                    includePrerelease: _includePreview,
                    includeUnlisted: true,
                    _cacheContext,
                    NullLogger.Instance,
                    cancellationToken).ConfigureAwait(false);

                if (!foundPackages.Any())
                {
                    Console.WriteLine($"Package {packageIdentifier} was not found.");
                    return default;
                }

                if (foundPackages.Any(package => package.IsListed))
                {
                    IPackageSearchMetadata latestPackage = foundPackages
                        .Where(package => package.IsListed)
                        .Aggregate((max, current) =>
                        {
                            return current.Identity.Version > max.Identity.Version ? current : max;
                        });
                    return (new NuGetPackInfo(latestPackage), false);
                }

                IPackageSearchMetadata latestUnlistedPackage = foundPackages
                 .Aggregate((max, current) =>
                 {
                     return current.Identity.Version > max.Identity.Version ? current : max;
                 });

                return (new NuGetPackInfo(latestUnlistedPackage), true);

            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get information about package {packageIdentifier}, details: {ex}");
                return default;
            }
        }

        public void Dispose() => _httpClient.Dispose();

        private class NuGetPackInfo : ITemplatePackageInfo
        {
            internal NuGetPackInfo(IPackageSearchMetadata packageSearchMetadata)
            {
                Name = packageSearchMetadata.Identity.Id;
                Version = packageSearchMetadata.Identity.Version.ToString();
                TotalDownloads = packageSearchMetadata.DownloadCount ?? 0;
                Reserved = packageSearchMetadata.PrefixReserved;
                Description = packageSearchMetadata.Description;
                IconUrl = packageSearchMetadata.IconUrl?.ToString();
            }

            public string Name { get; }

            public string? Version { get; }

            public long TotalDownloads { get; }

            public IReadOnlyList<string> Owners => Array.Empty<string>();

            public bool Reserved { get; }

            public string? Description { get; }

            public string? IconUrl { get; }
        }
    }
}
