using System;
using System.Collections.Generic;
using Microsoft.TemplateSearch.TemplateDiscovery.AdditionalData;
using Microsoft.TemplateSearch.TemplateDiscovery.Filters;
using Microsoft.TemplateSearch.TemplateDiscovery.PackChecking;
using Microsoft.TemplateSearch.TemplateDiscovery.PackChecking.Reporting;
using Microsoft.TemplateSearch.TemplateDiscovery.PackProviders;

namespace Microsoft.TemplateSearch.TemplateDiscovery.Nuget
{
    public class NugetPackScraper
    {
        public static bool TryCreateDefaultNugetPackScraper(ScraperConfig config, out PackSourceChecker packSourceChecker)
        {
            List<IPackProvider> providers = new List<IPackProvider>
            {
                new NugetPackProvider("query-template", "q=template", config.BasePath, config.PageSize, config.RunOnlyOnePage, config.IncludePreviewPacks),
                new NugetPackProvider("query-package-type-template", "packageType=Template",  config.BasePath, config.PageSize, config.RunOnlyOnePage, config.IncludePreviewPacks)
            };

            List<Func<IDownloadedPackInfo, PreFilterResult>> preFilterList = new List<Func<IDownloadedPackInfo, PreFilterResult>>();

            if (!PreviouslyRejectedPackFilter.TryGetPreviouslySkippedPacks(config.PreviousRunBasePath, out HashSet<string> nonTemplatePacks))
            {
                Console.WriteLine("Unable to read results from the previous run.");
                packSourceChecker = null;
                return false;
            }

            preFilterList.Add(PreviouslyRejectedPackFilter.SetupPackFilter(nonTemplatePacks));
            if (!config.DontFilterOnTemplateJson)
            {
                preFilterList.Add(TemplateJsonExistencePackFilter.SetupPackFilter());
            }
            preFilterList.Add(SkipTemplatePacksFilter.SetupPackFilter());

            PackPreFilterer preFilterer = new PackPreFilterer(preFilterList);

            IReadOnlyList<IAdditionalDataProducer> additionalDataProducers = new List<IAdditionalDataProducer>()
            {
                new CliHostDataProducer()
            };

            packSourceChecker = new PackSourceChecker(providers, preFilterer, additionalDataProducers, config.SaveCandidatePacks);
            return true;
        }
    }
}
