using Microsoft.TemplateEngine.Abstractions.Installer;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Abstractions.TemplatesSources
{
    public interface IManagedTemplatesSourcesProvider : ITemplatesSourcesProvider
    {
        Task<IReadOnlyList<IManagedTemplatesSourceUpdate>> GetLatestVersions(IEnumerable<IManagedTemplatesSource> sources);
        Task<IReadOnlyList<InstallResult>> UpdateAsync(IEnumerable<IManagedTemplatesSourceUpdate> sources);
        Task<UninstallResult> UninstallAsync(IManagedTemplatesSource source);
        Task<InstallResult> InstallAsync(InstallRequest installRequest);
    }
}
