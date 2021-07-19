// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateSearch.Common
{
    public class PackInfo
    {
        public PackInfo(string name, string version, long totalDownloads = 0, IReadOnlyList<string>? owners = null, bool verified = false)
        {
            Name = name;
            Version = version;
            TotalDownloads = totalDownloads;
            Verified = verified;
            Owners = owners ?? Array.Empty<string>();
        }

        internal PackInfo(string name, JObject dataObject)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"{nameof(name)} cannot be null or whitespace.", nameof(name));
            }

            Name = name;
            Version = dataObject.ToString(nameof(Version))
                ?? throw new ArgumentException($"{nameof(dataObject)} doesn't have {nameof(Version)} property.", nameof(dataObject));

            TotalDownloads = dataObject.ToInt32(nameof(TotalDownloads));
            Verified = dataObject.ToBool(nameof(Verified));
            Owners = dataObject.Get<JObject>(nameof(Owners)).JTokenStringOrArrayToCollection(Array.Empty<string>());
        }

        public string Name { get; }

        public string Version { get; }

        public long TotalDownloads { get; }

        public IReadOnlyList<string> Owners { get; }

        public bool Verified { get; }
    }

    public class PackInfoEqualityComparer : IEqualityComparer<PackInfo>
    {
        public bool Equals(PackInfo x, PackInfo y)
        {
            return string.Equals(x.Name, y.Name) && string.Equals(x.Version, y.Version);
        }

        public int GetHashCode(PackInfo info)
        {
            return info.Name.GetHashCode() ^ info.Version.GetHashCode();
        }
    }
}
