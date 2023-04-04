// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal interface IDownloader
    {
        Task<NuGetPackageInfo> DownloadPackageAsync(string downloadPath, string identifier, string? version = null, IEnumerable<string>? additionalSources = null, bool force = false, CancellationToken cancellationToken = default);
    }

    internal class NuGetPackageInfo
    {
        public NuGetPackageInfo(string author, string owners, bool trusted, string fullPath, string? nuGetSource, string packageIdentifier, string packageVersion, IEnumerable<PackageVulnerabilityMetadata>? vulnerabilities = null)
        {
            Author = author;
            Owners = owners;
            Trusted = trusted;
            FullPath = fullPath;
            NuGetSource = nuGetSource;
            PackageIdentifier = packageIdentifier;
            PackageVersion = packageVersion;
            PackageVulnerabilities = vulnerabilities;
        }

        public string Author { get; }

        public string Owners { get; }

        public bool Trusted { get; }

        public string FullPath { get; }

        public string? NuGetSource { get; }

        public string PackageIdentifier { get; }

        public string PackageVersion { get; }

        public IEnumerable<PackageVulnerabilityMetadata>? PackageVulnerabilities { get; }

        internal NuGetPackageInfo WithFullPath(string newFullPath)
        {
            return new NuGetPackageInfo(
                Author,
                Owners,
                Trusted,
                newFullPath,
                NuGetSource,
                PackageIdentifier,
                PackageVersion,
                Packagevulnerabilities);
        }
    }
}
