// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine;
using Microsoft.TemplateSearch.Common.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateSearch.TemplateDiscovery.NuGet
{
    internal class NuGetPackageSourceInfo : ITemplatePackageInfo, IEquatable<ITemplatePackageInfo>
    {
        internal NuGetPackageSourceInfo(string id, string version)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException($"'{nameof(id)}' cannot be null or whitespace.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentException($"'{nameof(version)}' cannot be null or whitespace.", nameof(version));
            }

            Name = id;
            Version = version;
        }

        [JsonProperty(PropertyName = "Id")]
        public string Name { get; private set; }

        [JsonProperty]
        public string Version { get; private set; }

        [JsonProperty]
        public long TotalDownloads { get; set; }

        internal static NuGetPackageSourceInfo FromJObject (JObject entry)
        {
            string id = entry.ToString("id") ?? throw new ArgumentException($"{nameof(entry)} doesn't have \"id\" property.", nameof(entry));
            string version = entry.ToString(nameof(Version)) ?? throw new ArgumentException($"{nameof(entry)} doesn't have {nameof(Version)} property.", nameof(entry));
            NuGetPackageSourceInfo sourceInfo = new NuGetPackageSourceInfo(id, version);
            sourceInfo.TotalDownloads = entry.ToInt32(nameof(TotalDownloads));
            return sourceInfo;
        }

#pragma warning disable SA1202 // Elements should be ordered by access
        public override bool Equals(object? obj)
#pragma warning restore SA1202 // Elements should be ordered by access
        {
            if (obj is NuGetPackageSourceInfo info)
            {
                return Name.Equals(info.Name, StringComparison.OrdinalIgnoreCase) && Version.Equals(info.Version, StringComparison.OrdinalIgnoreCase);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return (Name, Version).GetHashCode();
        }

        public bool Equals(ITemplatePackageInfo? other)
        {
            if (other == null)
            {
                return false;
            }

            return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) && Version.Equals(other.Version, StringComparison.OrdinalIgnoreCase);
        }
    }
}
