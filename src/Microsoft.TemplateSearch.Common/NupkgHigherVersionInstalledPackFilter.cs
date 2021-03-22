using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatesPackages;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateSearch.Common
{
    public class NupkgHigherVersionInstalledPackFilter : ISearchPackFilter
    {
        private readonly IReadOnlyList<IManagedTemplatesPackage> _existingTemplatesPackage;
        private IReadOnlyDictionary<string, string> _existingTemplatesPackageFilterData;
        private bool _isInitialized;

        public NupkgHigherVersionInstalledPackFilter(IReadOnlyList<IManagedTemplatesPackage> existingInstallDecriptors)
        {
            _existingTemplatesPackage = existingInstallDecriptors;
            _isInitialized = false;
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            Dictionary<string, string> filterData = new Dictionary<string, string>();

            foreach (IManagedTemplatesPackage descriptor in _existingTemplatesPackage)
            {
                filterData[descriptor.Identifier] = descriptor.Version;
            }

            _existingTemplatesPackageFilterData = filterData;

            _isInitialized = true;
        }

        public bool ShouldPackBeFiltered(string candidatePackName, string candidatePackVersion)
        {
            EnsureInitialized();

            if (!_existingTemplatesPackageFilterData.TryGetValue(candidatePackName, out string existingPackVersion))
            {
                // no existing install of this pack - don't filter it
                return false;
            }

            return existingPackVersion != candidatePackVersion;
        }
    }
}
