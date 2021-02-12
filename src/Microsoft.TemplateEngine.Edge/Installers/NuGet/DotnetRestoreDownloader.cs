// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions.Installer;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class DotnetRestoreDownloader : IDownloader
    {
        public bool CanDownloadPackage(InstallRequest installRequest)
        {
            throw new NotImplementedException();
        }

        public Task<NuGetPackageInfo> DownloadPackageAsync(InstallRequest installRequest, string downloadPath)
        {
            throw new NotImplementedException();
        }
    }
}
