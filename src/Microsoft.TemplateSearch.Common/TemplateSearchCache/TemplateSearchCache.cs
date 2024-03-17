// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.TemplateSearch.Common
{
    internal partial class TemplateSearchCache
    {
        private const string CurrentVersion = "2.0";

        internal TemplateSearchCache(IReadOnlyList<TemplatePackageSearchData> data, DateTimeOffset lastUpdateTime)
        {
            // when creating from freshly-generated data, order the results for clarity
            TemplatePackages = data.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToArray();
            Version = CurrentVersion;
            LastUpdateTime = lastUpdateTime;
        }

        private TemplateSearchCache(IReadOnlyList<TemplatePackageSearchData> data, string version, DateTimeOffset? lastUpdateTime)
        {
            // don't order results when creating from a read file
            TemplatePackages = data;
            Version = version;
            LastUpdateTime = lastUpdateTime;
        }

        [JsonProperty]
        internal IReadOnlyList<TemplatePackageSearchData> TemplatePackages { get; }

        [JsonProperty]
        internal string Version { get; private set; }

        [JsonProperty]
        internal DateTimeOffset? LastUpdateTime { get; private set; }
    }
}
