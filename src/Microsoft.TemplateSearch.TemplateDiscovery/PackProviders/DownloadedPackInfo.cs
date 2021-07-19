// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateSearch.Common;

namespace Microsoft.TemplateSearch.TemplateDiscovery.PackProviders
{
    public class DownloadedPackInfo
    {
        internal DownloadedPackInfo(PackInfo info, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or whitespace.", nameof(filePath));
            }

            PackInfo = info ?? throw new ArgumentNullException(nameof(info));
            Path = filePath;
        }

        public PackInfo PackInfo { get; private set; }

        public string Path { get; private set; }

    }
}
