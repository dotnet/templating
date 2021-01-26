using Microsoft.TemplateEngine.Abstractions;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal interface IUpdateChecker
    {
        Task<SemanticVersion> GetLatestVersionAsync(NuGetManagedTemplatesSource source);

        bool CanCheckForUpdate(NuGetManagedTemplatesSource source);
    }
}
