// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateSearch.Common.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.TemplateSearch.Common
{
    [Obsolete("The class is deprecated. Use TemplateSearchCache instead to create search cache data.")]
    internal class PackInfo : ITemplatePackageInfo
    {
        internal PackInfo(string name, string version)
        {
            Name = name;
            Version = version;
        }

        internal PackInfo(string name, string version, long totalDownloads, IEnumerable<string> owners, bool reserved = false)
        {
            Name = name;
            Version = version;
            TotalDownloads = totalDownloads;
            Owners = owners.ToList();
            Reserved = reserved;
        }

        [JsonProperty]
        public string Name { get; }

        [JsonProperty]
        public string Version { get; }

        [JsonProperty]
        public long TotalDownloads { get; }

        [JsonProperty]
        public IReadOnlyList<string> Owners { get; } = Array.Empty<string>();

        [JsonProperty]
        public bool Reserved { get; }

        //not supported for v1
        public string? Description => null;

        //not supported for v1
        public string? IconUrl => null;
    }
}
