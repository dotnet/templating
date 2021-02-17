using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateSearch.Common
{
    public class NupkgHigherVersionInstalledPackFilter : ISearchPackFilter
    {
        private readonly IReadOnlyList<IManagedTemplatesSource> _existingTemplatesSource;
        private IReadOnlyDictionary<string, string> _existingTemplatesSourceFilterData;
        private bool _isInitialized;

        public NupkgHigherVersionInstalledPackFilter(IReadOnlyList<IManagedTemplatesSource> existingInstallDecriptors)
        {
            _existingTemplatesSource = existingInstallDecriptors;
            _isInitialized = false;
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            Dictionary<string, string> filterData = new Dictionary<string, string>();

            foreach (IManagedTemplatesSource descriptor in _existingTemplatesSource)
            {
                filterData[descriptor.Identifier] = descriptor.Version;
            }

            _existingTemplatesSourceFilterData = filterData;

            _isInitialized = true;
        }

        public bool ShouldPackBeFiltered(string candidatePackName, string candidatePackVersion)
        {
            EnsureInitialized();

            if (!_existingTemplatesSourceFilterData.TryGetValue(candidatePackName, out string existingPackVersion))
            {
                // no existing install of this pack - don't filter it
                return false;
            }

            return existingPackVersion != candidatePackVersion;
        }
    }
}
