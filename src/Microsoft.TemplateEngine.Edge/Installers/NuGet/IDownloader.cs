// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions.Installer;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal interface IDownloader
    {
        bool CanDownloadPackage(InstallRequest installRequest);

        Task<NuGetPackageInfo> DownloadPackageAsync(InstallRequest installRequest, string downloadPath);
    }

    internal struct NuGetPackageInfo
    {
        internal string Author;
        internal string FullPath;
        internal string NuGetSource;
        internal string PackageIdentifier;
        internal string PackageVersion;
    }
}
