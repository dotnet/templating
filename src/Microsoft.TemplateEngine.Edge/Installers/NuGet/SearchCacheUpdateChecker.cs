using Microsoft.TemplateEngine.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class SearchCacheUpdateChecker : IUpdateChecker
    {
        public bool CanCheckForUpdate(NuGetManagedTemplatesSource source)
        {
            throw new NotImplementedException();
        }

        public Task<SemanticVersion> GetLatestVersionAsync(NuGetManagedTemplatesSource source)
        {
            throw new NotImplementedException();
        }
    }
}
