using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.TemplateSearch.TemplateDiscovery.Nuget;
using Microsoft.TemplateSearch.TemplateDiscovery.PackProviders;

namespace Microsoft.TemplateSearch.TemplateDiscovery
{
    // Ad-hoc for test purposes (for now).
    // Not referenced by any live code, but could becone the basis for a local source scraper implementation
    public class LocalFileSourcePackProvider : IPackProvider
    {
        private static readonly string PathToPacks = @"";

        public IEnumerable<IPackInfo> CandidatePacks
        {
            get
            {
                List<IPackInfo> packs = new List<IPackInfo>();

                foreach (string packFileName in Directory.EnumerateFiles(PathToPacks))
                {
                    NugetPackInfo packInfo = new NugetPackInfo()
                    {
                        VersionedPackageIdentity = Path.GetFileName(packFileName),
                        Id = Path.GetFileName(packFileName),
                        Version = "0.0",
                        Path = packFileName
                    };

                    packs.Add(packInfo);
                }

                return packs;
            }
        }

        public void DeleteDownloadedPacks()
        {
            throw new NotImplementedException();
        }
    }
}
