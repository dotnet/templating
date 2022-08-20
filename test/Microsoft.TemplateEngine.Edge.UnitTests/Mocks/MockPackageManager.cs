﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Edge.Installers.NuGet;
using Microsoft.TemplateEngine.TestHelper;

namespace Microsoft.TemplateEngine.Edge.UnitTests.Mocks
{
    internal class MockPackageManager : IDownloader, IUpdateChecker
    {
        internal const string DefaultFeed = "test_feed";
        private PackageManager? _packageManager;
        private readonly string? _packageToPack;

        internal MockPackageManager()
        {
        }

        internal MockPackageManager(PackageManager packageManager, string packageToPack)
        {
            _packageManager = packageManager;
            _packageToPack = packageToPack;
        }

        public Task<NuGetPackageInfo> DownloadPackageAsync(string downloadPath, string identifier, string? version = null, IEnumerable<string>? additionalSources = null, bool force = false, CancellationToken cancellationToken = default)
        {
            // names of exceptions throw them for test purposes
            switch (identifier)
            {
                case nameof(InvalidNuGetSourceException): throw new InvalidNuGetSourceException("test message");
                case nameof(DownloadException): throw new DownloadException(identifier, version ?? string.Empty, new[] { DefaultFeed });
                case nameof(PackageNotFoundException): throw new PackageNotFoundException(identifier, new[] { DefaultFeed });
                case nameof(Exception): throw new Exception("Generic error");
            }

            if (_packageManager == null)
            {
                throw new InvalidOperationException($"{nameof(_packageManager)} was not initialized");
            }
            if (_packageToPack == null)
            {
                throw new InvalidOperationException($"{nameof(_packageToPack)} was not initialized");
            }

            string testPackageLocation = _packageManager.PackNuGetPackage(_packageToPack);
            string targetFileName;
            if (string.IsNullOrWhiteSpace(version))
            {
                targetFileName = Path.GetFileName(testPackageLocation);
            }
            else
            {
                targetFileName = $"{Path.GetFileNameWithoutExtension(testPackageLocation)}.{version}.nupkg";
            }
            File.Copy(testPackageLocation, Path.Combine(downloadPath, targetFileName));
            return Task.FromResult(new NuGetPackageInfo("Microsoft", Path.Combine(downloadPath, targetFileName), DefaultFeed, identifier, version ?? string.Empty));
        }

        public Task<(string LatestVersion, bool IsLatestVersion)> GetLatestVersionAsync(string identifier, string? version = null, string? additionalNuGetSource = null, CancellationToken cancellationToken = default)
        {
            // names of exceptions throw them for test purposes
            switch (identifier)
            {
                case nameof(InvalidNuGetSourceException): throw new InvalidNuGetSourceException("test message");
                case nameof(DownloadException): throw new DownloadException(identifier, version ?? string.Empty, new[] { DefaultFeed });
                case nameof(PackageNotFoundException): throw new PackageNotFoundException(identifier, new[] { DefaultFeed });
                case nameof(Exception): throw new Exception("Generic error");
            }

            return Task.FromResult(("1.0.0", version != "1.0.0"));
        }
    }
}
