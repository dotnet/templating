// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateSearch.Common;
using Microsoft.TemplateSearch.TemplateDiscovery.NuGet;

namespace Microsoft.TemplateSearch.TemplateDiscovery.PackProviders
{
    internal interface IPackProvider
    {
        string Name { get; }

        IAsyncEnumerable<PackInfo> GetCandidatePacksAsync(CancellationToken token);

        Task<DownloadedPackInfo?> DownloadPackageAsync(PackInfo packinfo, CancellationToken token);

        Task<int> GetPackageCountAsync(CancellationToken token);

        Task DeleteDownloadedPacksAsync();
    }
}
