// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class NuGetManagedTemplatesSource : IManagedTemplatesSource
    {
        public const string AuthorKey = "Author";
        public const string LocalPackageKey = "LocalPackage";
        public const string NuGetSourceKey = "NuGetSource";
        public const string PackageIdKey = "PackageId";
        public const string PackageVersionKey = "Version";

        public NuGetManagedTemplatesSource(IInstaller installer, string mountPoint, Dictionary<string, string> details)
        {
            Installer = installer;
            MountPointUri = mountPoint;
            Details = details;
        }

        public string Author => Details.TryGetValue(AuthorKey, out string author) ? author : null;
        public string Identifier => Details.TryGetValue(PackageIdKey, out string identifier) ? identifier : null;
        public IInstaller Installer { get; }
        public DateTime LastChangeTime { get; }
        public bool LocalPackage => Details.TryGetValue(LocalPackageKey, out string isLocalPackage) && bool.TryParse(isLocalPackage, out bool result) ? result : false;
        public string MountPointUri { get; }
        public string NuGetSource => Details.TryGetValue(NuGetSourceKey, out string nugetSource) ? nugetSource : null;
        public bool PrivateFeed => NuGetSource != NuGetApiPackageManager.PublicNuGetFeed;
        public ITemplatesSourcesProvider Provider => Installer.Provider;
        public string Version => Details.TryGetValue(PackageVersionKey, out string version) ? version : null;
        internal IReadOnlyDictionary<string, string> Details { get; }
        public IReadOnlyDictionary<string, string> GetDisplayDetails()
        {
            Dictionary<string, string> details = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(Author))
            {
                details[AuthorKey] = Author;
            }
            return details;
        }
    }
}
