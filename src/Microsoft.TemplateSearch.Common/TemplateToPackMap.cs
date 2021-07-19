// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.TemplateSearch.Common
{
    public class TemplateToPackMap
    {
        private readonly IReadOnlyDictionary<string, PackInfo> _identityToPackMap;

        protected TemplateToPackMap(Dictionary<string, PackInfo> identityToPackMap)
        {
            _identityToPackMap = identityToPackMap;
        }

        public static TemplateToPackMap FromPackToTemplateDictionary(IReadOnlyDictionary<string, PackToTemplateEntry> templateDictionary)
        {
            Dictionary<string, PackInfo> identityToPackMap = new Dictionary<string, PackInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, PackToTemplateEntry> entry in templateDictionary)
            {
                PackInfo packInfo = entry.Value.PackInfo;

                foreach (TemplateIdentificationEntry templateIdentityInfo in entry.Value.TemplateIdentificationEntry)
                {
                    // Empty entries for the identity or group identity are authoring errors.
                    // Here, they're just filtered to prevent them from being matched as empty string.

                    if (!string.IsNullOrEmpty(templateIdentityInfo.Identity))
                    {
                        identityToPackMap[templateIdentityInfo.Identity] = packInfo;
                    }
                }
            }

            return new TemplateToPackMap(identityToPackMap);
        }

        public bool TryGetPackInfoForTemplateIdentity(string templateName, out PackInfo? packAndVersion)
        {
            if (_identityToPackMap.TryGetValue(templateName, out packAndVersion))
            {
                return true;
            }

            packAndVersion = null;
            return false;
        }
    }
}
