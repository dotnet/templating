using System.Collections.Generic;

namespace Microsoft.TemplateSearch.TemplateDiscovery.PackProviders
{
    public interface IPackProvider
    {
        IEnumerable<IInstalledPackInfo> CandidatePacks { get; }

        void DeleteDownloadedPacks();
    }
}
