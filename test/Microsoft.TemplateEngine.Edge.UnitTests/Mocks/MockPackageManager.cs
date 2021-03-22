// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Edge.Installers.NuGet;
using Microsoft.TemplateEngine.TestHelper;

namespace Microsoft.TemplateEngine.Edge.UnitTests.Mocks
{
    internal class MockPackageManager : IDownloader, IUpdateChecker
    {
        internal const string DefaultFeed = "test_feed";
        private PackageManager _packageManager;

        internal MockPackageManager ()
        {

        }

        internal MockPackageManager (PackageManager packageManager)
        {
            _packageManager = packageManager;
        }

        public Task<NuGetPackageInfo> DownloadPackageAsync(InstallRequest installRequest, string downloadPath, CancellationToken cancellationToken)
        {
            // names of exceptions throw them for test purposes
            switch (installRequest.Identifier)
            {
                case nameof(InvalidNuGetSourceException): throw new InvalidNuGetSourceException("test message");
                case nameof(DownloadException): throw new DownloadException(installRequest.Identifier, installRequest.Version, new[] { DefaultFeed });
                case nameof(PackageNotFoundException): throw new PackageNotFoundException(installRequest.Identifier, new[] { DefaultFeed });
                case nameof(Exception): throw new Exception("Generic error");
            }

            string testPackageLocation = _packageManager.PackTestTemplatesNuGetPackage();
            string targetFileName;
            if (string.IsNullOrWhiteSpace(installRequest.Version))
            {
                targetFileName = Path.GetFileName(testPackageLocation);
            }
            else
            {
                targetFileName = $"{Path.GetFileNameWithoutExtension(testPackageLocation)}.{installRequest.Version}.nupkg";
            }
            File.Copy(testPackageLocation, Path.Combine(downloadPath, targetFileName));
            return Task.FromResult(new NuGetPackageInfo
            {
                Author = "Microsoft",
                FullPath = Path.Combine(downloadPath, targetFileName),
                PackageIdentifier = installRequest.Identifier,
                PackageVersion = installRequest.Version,
                NuGetSource = installRequest.Details?.ContainsKey(InstallerConstants.NuGetSourcesKey) ?? false ? installRequest.Details[InstallerConstants.NuGetSourcesKey] : DefaultFeed
            });
        }

        public Task<CheckUpdateResult> GetLatestVersionAsync(NuGetManagedTemplatesSource source, CancellationToken cancellationToken)
        {
            // names of exceptions throw them for test purposes
            switch (source.Identifier)
            {
                case nameof(InvalidNuGetSourceException): throw new InvalidNuGetSourceException("test message");
                case nameof(DownloadException): throw new DownloadException(source.Identifier, source.Version, new[] { DefaultFeed });
                case nameof(PackageNotFoundException): throw new PackageNotFoundException(source.Identifier, new[] { DefaultFeed });
                case nameof(Exception): throw new Exception("Generic error");
            }

            return Task.FromResult(CheckUpdateResult.CreateSuccess(source, "1.0.0", source.Version != "1.0.0"));
        }
    }
}
