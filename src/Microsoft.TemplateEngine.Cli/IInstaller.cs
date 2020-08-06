using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli
{
    public interface IInstaller : IInstallerBase
    {
        void InstallPackages(IEnumerable<string> installationRequests, IList<string> nuGetSources);

        void InstallPackages(IEnumerable<string> installationRequests, IList<string> nuGetSources, bool debugAllowDevInstall);

        void InstallPackages(IEnumerable<string> installationRequests, IList<string> nuGetSources, bool debugAllowDevInstall, bool interactive);

        /// <param name="optionalWorkloadRequests">Requests from <paramref name="installationRequests"/> which are part of an optional workload.</param>
        void InstallPackages(IEnumerable<string> installationRequests, IList<string> optionalWorkloadRequests, IList<string> nuGetSources);

        /// <param name="optionalWorkloadRequests">Requests from <paramref name="installationRequests"/> which are part of an optional workload.</param>
        void InstallPackages(IEnumerable<string> installationRequests, IList<string> optionalWorkloadRequests, IList<string> nuGetSources, bool debugAllowDevInstall);

        /// <param name="optionalWorkloadRequests">Requests from <paramref name="installationRequests"/> which are part of an optional workload.</param>
        void InstallPackages(IEnumerable<string> installationRequests, IList<string> optionalWorkloadRequests, IList<string> nuGetSources, bool debugAllowDevInstall, bool interactive);
    }
}
