using System.Collections.Generic;

namespace Microsoft.TemplateSearch.TemplateDiscovery.PackProviders
{
    public interface IPackProvider
    {
        IEnumerable<IPackInfo> CandidatePacks { get; }
    }
}
