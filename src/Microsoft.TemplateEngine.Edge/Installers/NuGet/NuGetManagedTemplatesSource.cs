using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using System;
using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class NuGetManagedTemplatesSource : IManagedTemplatesSource
    {
        public const string NuGetSourceKey = "NuGetSource";
        public const string PackageIdKey = "PackageId";
        public const string PackageVersionKey = "Version";
        public const string LocalPackageKey = "LocalPackage";
        public const string AuthorKey = "Author";

        static List<string> detailKeysDisplayOrder = new List<string>() { AuthorKey, NuGetSourceKey };

        public NuGetManagedTemplatesSource(IInstaller installer, string mountPoint, Dictionary<string, string> details)
        {
            Installer = installer;
            MountPointUri = mountPoint;
            Details = details;
        }

        public string Identifier => Details.TryGetValue(PackageIdKey, out string identifier) ? identifier : null;

        public string Version => Details.TryGetValue(PackageVersionKey, out string version) ? version : null;

        public IReadOnlyDictionary<string, string> Details { get; }

        public IReadOnlyList<string> DetailKeysDisplayOrder => detailKeysDisplayOrder;

        public DateTime LastChangeTime { get; }

        public string MountPointUri { get; }

        public string NuGetSource => Details.TryGetValue(NuGetSourceKey, out string nugetSource) ? nugetSource : null;

        public string Author => Details.TryGetValue(AuthorKey, out string author) ? author : null;

        public ITemplatesSourcesProvider Provider => Installer.Provider;

        public bool LocalPackage => Details.TryGetValue(LocalPackageKey, out string isLocalPackage) && Boolean.TryParse(isLocalPackage, out bool result) ? result : false;

        public bool PrivateFeed => NuGetSource != NuGetApiPackageManager.PublicNuGetFeed;

        public IInstaller Installer { get; }
    }

}
