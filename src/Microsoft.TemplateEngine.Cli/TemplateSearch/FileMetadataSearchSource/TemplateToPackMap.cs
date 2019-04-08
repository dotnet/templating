using System.Collections.Generic;
using Microsoft.TemplateSearch.Common;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource
{
    internal class TemplateToPackMap
    {
        public static TemplateToPackMap FromPackToTemplateDictionary(IReadOnlyDictionary<string, PackToTemplateEntry> templateDictionary)
        {
            Dictionary<string, PackAndVersion> identityToPackMap = new Dictionary<string, PackAndVersion>();
            Dictionary<string, PackAndVersion> groupIdentityToPackMap = new Dictionary<string, PackAndVersion>();

            foreach (KeyValuePair<string, PackToTemplateEntry> entry in templateDictionary)
            {
                PackAndVersion packAndVersion = new PackAndVersion(entry.Key, entry.Value.Version);

                foreach (TemplateIdentificationEntry templateIdentityInfo in entry.Value.TemplateIdentificationEntry)
                {
                    // Empty entries for the identity or group identity are authoring errors.
                    // Here, they're just filtered to prevent them from being matched as enmpty string.

                    if (!string.IsNullOrEmpty(templateIdentityInfo.Identity))
                    {
                        identityToPackMap[templateIdentityInfo.Identity] = packAndVersion;
                    }

                    if (!string.IsNullOrEmpty(templateIdentityInfo.GroupIdentity))
                    {
                        groupIdentityToPackMap[templateIdentityInfo.GroupIdentity] = packAndVersion;
                    }
                }
            }

            return new TemplateToPackMap(identityToPackMap, groupIdentityToPackMap);
        }

        protected TemplateToPackMap(Dictionary<string, PackAndVersion> identityToPackMap, Dictionary<string, PackAndVersion> groupIdentityToPackMap)
        {
            _identityToPackMap = identityToPackMap;
            _groupIdentityToPackMap = groupIdentityToPackMap;
        }

        private readonly IReadOnlyDictionary<string, PackAndVersion> _identityToPackMap;
        private readonly IReadOnlyDictionary<string, PackAndVersion> _groupIdentityToPackMap;

        public bool TryGetPackInfoForTemplateIdentity(string templateName, out PackAndVersion packAndVersion)
        {
            if (_identityToPackMap.TryGetValue(templateName, out packAndVersion))
            {
                return true;
            }

            packAndVersion = PackAndVersion.Empty;
            return false;
        }

        public bool TryGetPackInfoForTemplateGroupIdentity(string templateName, out PackAndVersion packAndVersion)
        {
            if (_groupIdentityToPackMap.TryGetValue(templateName, out packAndVersion))
            {
                return true;
            }

            packAndVersion = PackAndVersion.Empty;
            return false;
        }
    }
}
