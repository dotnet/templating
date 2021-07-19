// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.TemplateSearch.Common;
using Microsoft.TemplateSearch.TemplateDiscovery.NuGet;

namespace Microsoft.TemplateSearch.TemplateDiscovery.PackProviders
{
    internal class TestPackProvider : IPackProvider
    {
        private readonly string _folder;

        internal TestPackProvider(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                throw new ArgumentException($"'{nameof(folder)}' cannot be null or whitespace.", nameof(folder));
            }
            _folder = folder;
        }

        public string Name => "TestProvider";

        public Task DeleteDownloadedPacksAsync()
        {
            //do nothing - do not remove test packs
            return Task.FromResult(0);
        }

        public Task<DownloadedPackInfo?> DownloadPackageAsync(PackInfo packinfo, CancellationToken token)
        {
            return Task.FromResult((DownloadedPackInfo?)new DownloadedPackInfo(packinfo, packinfo.Name));
        }

        public async IAsyncEnumerable<PackInfo> GetCandidatePacksAsync([EnumeratorCancellation]CancellationToken token)
        {
            var directoryInfo = new DirectoryInfo(_folder);

            foreach (FileInfo package in directoryInfo.EnumerateFiles("*.nupkg", SearchOption.AllDirectories))
            {
                yield return new PackInfo(package.FullName, "1.0");
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }

        public Task<int> GetPackageCountAsync(CancellationToken token)
        {
            return Task.FromResult(new DirectoryInfo(_folder).EnumerateFiles("*.nupkg", SearchOption.AllDirectories).Count());
        }
    }
}
