using Microsoft.TemplateEngine.Abstractions;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal interface IUpdateChecker
    {
        Task<string> GetLatestVersionAsync(NuGetManagedTemplatesSource source);

        bool CanCheckForUpdate(NuGetManagedTemplatesSource source);
    }
}
