using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateSearch.TemplateDiscovery.AdditionalData;
using Microsoft.TemplateSearch.TemplateDiscovery.PackChecking.Reporting;
using Microsoft.TemplateSearch.TemplateDiscovery.PackProviders;

namespace Microsoft.TemplateSearch.TemplateDiscovery.PackChecking
{
    public class PackSourceChecker
    {
        private readonly IEnumerable<IPackProvider> _packProviders;
        private readonly PackPreFilterer _packPreFilterer;
        private readonly PackChecker _packChecker;
        private readonly IReadOnlyList<IAdditionalDataProducer> _additionalDataProducers;
        private readonly bool _saveCandidatePacks;

        public PackSourceChecker(IEnumerable<IPackProvider> packProviders, PackPreFilterer packPreFilterer, IReadOnlyList<IAdditionalDataProducer> additionalDataProducers, bool saveCandidatePacks)
        {
            _packProviders = packProviders;
            _packPreFilterer = packPreFilterer;
            _additionalDataProducers = additionalDataProducers;
            _saveCandidatePacks = saveCandidatePacks;

            _packChecker = new PackChecker();
        }

        public async Task<PackSourceCheckResult> CheckPackagesAsync()
        {
            List<PackCheckResult> checkResultList = new List<PackCheckResult>();
            HashSet<string> alreadySeenTemplateIdentities = new HashSet<string>();

            foreach (IPackProvider packProvider in _packProviders)
            {
                Console.WriteLine($"Processing provider pack {packProvider.Name}");
                int count = 0;
                Console.WriteLine($"{await packProvider.GetPackageCountAsync().ConfigureAwait(false)} packs discovered, starting processing");

                await foreach (IInstalledPackInfo packInfo in packProvider.GetCandidatePacksAsync().ConfigureAwait(false))
                {
                    PackCheckResult preFilterResult = PrefilterPackInfo(packInfo);
                    if (preFilterResult.PreFilterResults.ShouldBeFiltered)
                    {
                        Verbose.WriteLine($"{packInfo.Id}::{packInfo.Version} is skipped, {preFilterResult.PreFilterResults.Reason}");
                        checkResultList.Add(preFilterResult);
                    }
                    else
                    {
                        PackCheckResult packCheckResult = _packChecker.TryGetTemplatesInPack(packInfo, _additionalDataProducers, alreadySeenTemplateIdentities);
                        checkResultList.Add(packCheckResult);


                        Verbose.WriteLine($"{packCheckResult.PackInfo.Id}::{packCheckResult.PackInfo.Version} was processed");
                        if (packCheckResult.FoundTemplates.Any())
                        {
                            Verbose.WriteLine("Found templates:");
                            foreach (ITemplateInfo template in packCheckResult.FoundTemplates)
                            {
                                Verbose.WriteLine($"  - {template.Identity} ({template.ShortName}) by {template.Author}, group: {(string.IsNullOrWhiteSpace(template.GroupIdentity) ? "<not set>" : template.GroupIdentity)}, precedence: {template.Precedence}");
                            }
                        }
                        else
                        {
                            Verbose.WriteLine("No templates were found");
                        }


                        // Record the found identities - to skip these templates when checking additional packs.
                        // Some template identities are the same in multiple packs on nuget.
                        // For this scraper, first in wins.
                        alreadySeenTemplateIdentities.UnionWith(packCheckResult.FoundTemplates.Select(t => t.Identity));
                    }

                    ++count;
                    if ((count % 10) == 0)
                    {
                        Console.WriteLine($"{count} packs processed");
                    }
                }
                Console.WriteLine($"Processing provider pack {packProvider.Name} finished.");
            }
            Console.WriteLine("All packs processed");

            if (!_saveCandidatePacks)
            {
                Console.WriteLine("Removing downloaded packs");
                foreach (IPackProvider provider in _packProviders)
                {
                    provider.DeleteDownloadedPacks();
                }
                Console.WriteLine("Downloaded packs were removed");
            }


            return new PackSourceCheckResult(checkResultList, _additionalDataProducers);
        }

        private PackCheckResult PrefilterPackInfo(IDownloadedPackInfo packInfo)
        {
            PreFilterResultList preFilterResult = _packPreFilterer.FilterPack(packInfo);
            return new PackCheckResult(packInfo, preFilterResult);
        }
    }
}
