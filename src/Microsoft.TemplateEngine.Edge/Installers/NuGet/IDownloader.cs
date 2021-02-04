using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal interface IDownloader 
    {
        Task<DownloadResult> DownloadPackageAsync(InstallRequest installRequest, string downloadPath);

        bool CanDownloadPackage(InstallRequest installRequest);
    }

    internal struct DownloadResult
    {
        internal string NuGetSource;
        internal string PackageIdentifier;
        internal string PackageVersion;
        internal string Author;
        internal string FullPath;
    }
}
