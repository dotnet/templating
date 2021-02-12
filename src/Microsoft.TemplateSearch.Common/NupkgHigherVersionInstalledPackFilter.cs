using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateSearch.Common
{
    public class NupkgHigherVersionInstalledPackFilter : ISearchPackFilter
    {
        private readonly IReadOnlyList<IManagedTemplatesSource> _existingInstallDescriptors;
        private IReadOnlyDictionary<string, SemanticVersion> _existingInstallDescriptorFilterData;
        private bool _isInitialized;

        public NupkgHigherVersionInstalledPackFilter(IReadOnlyList<IManagedTemplatesSource> existingInstallDecriptors)
        {
            _existingInstallDescriptors = existingInstallDecriptors;
            _isInitialized = false;
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            Dictionary<string, SemanticVersion> filterData = new Dictionary<string, SemanticVersion>();

            foreach (IManagedTemplatesSource descriptor in _existingInstallDescriptors)
            {
                filterData[descriptor.Identifier] = descriptor.Version;
            }

            _existingInstallDescriptorFilterData = filterData;

            _isInitialized = true;
        }

        public bool ShouldPackBeFiltered(string candidatePackName, string candidatePackVersion)
        {
            EnsureInitialized();

            if (!_existingInstallDescriptorFilterData.TryGetValue(candidatePackName, out SemanticVersion existingPackVersion))
            {
                // no existing install of this pack - don't filter it
                return false;
            }

            if (!SemanticVersion.TryParse(candidatePackVersion, out SemanticVersion candidateVersion))
            {
                // The candidate pack version didn't parse. So not filtering it - this might want to be revisited.
                // Realistically, this probably can't happen.
                return false;
            }

            return existingPackVersion >= candidateVersion;
        }
    }
}
