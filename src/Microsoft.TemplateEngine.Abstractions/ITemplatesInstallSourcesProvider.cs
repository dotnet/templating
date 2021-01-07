using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Abstractions
{
    public class TemplatesInstallSource
    {
        public DateTime LastWriteTime;
        public string Place;
        public Guid MountPointFactoryId;
    }

    public interface ITemplatesInstallSourcesProvider

    {
        Task<List<TemplatesInstallSource>> GetInstalledPackagesAsync(CancellationToken cancellationToken);
    }
    public interface ITemplatesInstallSourcesProviderFactory : IIdentifiedComponent
    {
        ITemplatesInstallSourcesProvider CreateProvider(IEngineEnvironmentSettings settings);
    }
}
