﻿using Microsoft.TemplateSearch.TemplateDiscovery.PackProviders;

namespace Microsoft.TemplateSearch.TemplateDiscovery.Nuget
{
    public class NugetPackInfo : IDownloadedPackInfo
    {
        public string VersionedPackageIdentity { get; set; }

        public string Id { get; set; }

        public string Version { get; set; }

        public string Path { get; set; }

        public int TotalDownloads { get; set; }
    }
}
