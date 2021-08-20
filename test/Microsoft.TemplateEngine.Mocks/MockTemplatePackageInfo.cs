﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateSearch.Common.Abstractions;

namespace Microsoft.TemplateEngine.Mocks
{
    public class MockTemplatePackageInfo : ITemplatePackageInfo
    {
        public MockTemplatePackageInfo(string name, string? version = null, long totalDownloads = 0, IEnumerable<string>? owners = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
            }

            Name = name;
            Version = version;
            TotalDownloads = totalDownloads;
            Owners = owners?.ToArray() ?? Array.Empty<string>();
        }

        public string Name { get; }

        public string? Version { get; }

        public long TotalDownloads { get; }

        public IReadOnlyList<string> Owners { get; }

        public bool Verified => false;
    }
}
