// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TemplateSearch.Common;
using Microsoft.TemplateSearch.TemplateDiscovery.AdditionalData;
using Microsoft.TemplateSearch.TemplateDiscovery.Filters;
using Microsoft.TemplateSearch.TemplateDiscovery.PackChecking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Protocol.Catalog;

namespace Microsoft.TemplateSearch.TemplateDiscovery.NuGet
{
    internal class NuGetPackSourceCheckerFactory : IPackCheckerFactory
    {
        private static readonly Dictionary<SupportedQueries, Func<PackageDetailsCatalogLeaf, bool>> SupportedProviders = new Dictionary<SupportedQueries, Func<PackageDetailsCatalogLeaf, bool>>()
        {
            { SupportedQueries.PackageTypeQuery, l => l.PackageTypes?.Any(t => t.Name.Equals("Template", StringComparison.OrdinalIgnoreCase)) ?? false },
            { SupportedQueries.TemplateQuery, l => l.PackageId?.Contains("template", StringComparison.OrdinalIgnoreCase) ?? l.Tags?.Any(t => t.Contains("template", StringComparison.OrdinalIgnoreCase)) ?? false }
        };

        public async Task<PackSourceChecker> CreatePackSourceCheckerAsync(CommandArgs config, CancellationToken cancellationToken)
        {
            List<IPackProvider> providers = new List<IPackProvider>();

            TemplateSearchCache? existingCache = config.DiffMode ? await LoadExistingCacheAsync(config, cancellationToken).ConfigureAwait(false) : null;
            var previousCache = existingCache?.LastUpdateTime ?? DateTimeOffset.MinValue;
            IEnumerable<FilteredPackageInfo>? knownPackages = config.DiffMode ? await LoadKnownPackagesListAsync(config, cancellationToken).ConfigureAwait(false) : null;

            if (!config.Queries.Any())
            {
                providers.AddRange(SupportedProviders.Select(kvp => new NuGetPackProvider(kvp.Key.ToString(), previousCache, kvp.Value, config.OutputPath, config.IncludePreviewPacks)));
            }
            else
            {
                foreach (SupportedQueries provider in config.Queries.Distinct())
                {
                    providers.Add(new NuGetPackProvider(provider.ToString(), previousCache, SupportedProviders[provider], config.OutputPath, config.IncludePreviewPacks));
                }
            }

            List<Func<IDownloadedPackInfo, PreFilterResult>> preFilterList = new List<Func<IDownloadedPackInfo, PreFilterResult>>();

            if (!config.DontFilterOnTemplateJson)
            {
                preFilterList.Add(TemplateJsonExistencePackFilter.SetupPackFilter());
            }
            preFilterList.Add(SkipTemplatePacksFilter.SetupPackFilter());
            preFilterList.Add(FilterNonMicrosoftAuthors.SetupPackFilter());

            PackPreFilterer preFilterer = new PackPreFilterer(preFilterList);

            IReadOnlyList<IAdditionalDataProducer> additionalDataProducers = new List<IAdditionalDataProducer>()
            {
                new CliHostDataProducer()
            };

            return new PackSourceChecker(providers, preFilterer, additionalDataProducers, config.SaveCandidatePacks, existingCache, knownPackages);
        }

        private static async Task<IEnumerable<FilteredPackageInfo>?> LoadKnownPackagesListAsync(CommandArgs config, CancellationToken cancellationToken)
        {
            Verbose.WriteLine($"Loading existing non-packages information.");
            const string uri = "https://dotnettemplating.blob.core.windows.net/search/nonTemplatePacks.json";

            FileInfo? fileLocation = config.DiffOverrideKnownPackagesLocation;
            if (fileLocation == null)
            {
                await DownloadUriToFileAsync(uri, "non-packages.json", cancellationToken).ConfigureAwait(false);
                fileLocation = new FileInfo("non-packages.json");
            }
            Verbose.WriteLine($"Opening {fileLocation.FullName}");

            using (var fileStream = fileLocation.OpenRead())
            using (var textReader = new StreamReader(fileStream, System.Text.Encoding.UTF8, true))
            using (var jsonReader = new JsonTextReader(textReader))
            {
                return new JsonSerializer().Deserialize<IEnumerable<FilteredPackageInfo>>(jsonReader);
            }
        }

        private static async Task<TemplateSearchCache?> LoadExistingCacheAsync(CommandArgs config, CancellationToken cancellationToken)
        {
            Verbose.WriteLine($"Loading existing cache information.");
            const string uri = "https://dotnet-templating.azureedge.net/search/NuGetTemplateSearchInfoVer2.json";

            FileInfo? cacheFileLocation = config.DiffOverrideSearchCacheLocation;

            if (cacheFileLocation == null)
            {
                await DownloadUriToFileAsync(uri, "currentSearchCache.json", cancellationToken).ConfigureAwait(false);
                cacheFileLocation = new FileInfo("currentSearchCache.json");
            }
            Verbose.WriteLine($"Opening {cacheFileLocation.FullName}");
            using (var fileStream = cacheFileLocation.OpenRead())
            using (var textReader = new StreamReader(fileStream, System.Text.Encoding.UTF8, true))
            using (var jsonReader = new JsonTextReader(textReader))
            {
                return TemplateSearchCache.FromJObject(JObject.Load(jsonReader), NullLogger.Instance, new Dictionary<string, Func<object, object>>() { { CliHostSearchCacheData.DataName, CliHostSearchCacheData.Reader } });
            }
        }

        private static async Task DownloadUriToFileAsync(string uri, string filePath, CancellationToken cancellationToken)
        {
            try
            {
                HttpClientHandler handler = new HttpClientHandler()
                {
                    CheckCertificateRevocationList = true,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                using (HttpClient client = new HttpClient(handler))
                {
                    using (HttpResponseMessage response = await client.GetAsync(uri, cancellationToken).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        string resultText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                        File.WriteAllText(filePath, resultText);
                        Verbose.WriteLine($"{uri} was successfully downloaded to {filePath}.");
                        return;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to download {0}, details: {1}", uri, e);
                throw;
            }
        }
    }
}
