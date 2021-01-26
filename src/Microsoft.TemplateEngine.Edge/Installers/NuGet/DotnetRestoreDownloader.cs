using Microsoft.TemplateEngine.Abstractions.Installer;
using System;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class DotnetRestoreDownloader : IDownloader
    {
        public bool CanDownloadPackage(InstallRequest installRequest)
        {
            throw new NotImplementedException();
        }

        public Task<DownloadResult> DownloadPackageAsync(InstallRequest installRequest, string downloadPath)
        {
            throw new NotImplementedException();
        }
    }
}
