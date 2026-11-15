// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Security;
using Microsoft.TemplateEngine.Edge.Installers.NuGet;
using Microsoft.TemplateEngine.TestHelper;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Xunit;

namespace Microsoft.TemplateEngine.Edge.UnitTests
{
    public class DotnetNuGetPackageVerifierTests : IClassFixture<EnvironmentSettingsHelper>, IClassFixture<PackageManager>
    {
        private readonly EnvironmentSettingsHelper _environmentSettingsHelper;
        private readonly PackageManager _packageManager;

        public DotnetNuGetPackageVerifierTests(EnvironmentSettingsHelper environmentSettingsHelper, PackageManager packageManager)
        {
            _environmentSettingsHelper = environmentSettingsHelper;
            _packageManager = packageManager;
        }

        [Fact]
        public async Task VerifyAsync_ValidSignature_Succeeds()
        {
            string packagePath = await _packageManager.GetNuGetPackage("Newtonsoft.Json", "13.0.3");

            await VerifyAsync(packagePath);
        }

        [Fact]
        public async Task VerifyAsync_InvalidSignature_IsRejected()
        {
            string signedPackage = await _packageManager.GetNuGetPackage("Newtonsoft.Json", "13.0.3");
            string packagePath = Path.Combine(_environmentSettingsHelper.CreateTemporaryFolder(), "Tampered Package.nupkg");
            File.Copy(signedPackage, packagePath);
            using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Update))
            {
                using var writer = new StreamWriter(archive.CreateEntry("tampered.txt").Open());
                writer.Write("Content added after signing.");
            }

            var exception = await Assert.ThrowsAsync<SecurityException>(() => VerifyAsync(packagePath));

            Assert.Contains("dotnet nuget verify --all", exception.Message);
            Assert.Contains("failed with exit code 1", exception.Message);
        }

        [Fact]
        public async Task VerifyAsync_MissingSignature_AcceptPolicy_Succeeds()
        {
            string packagePath = CreateUnsignedPackage();

            await VerifyAsync(packagePath, "accept");
        }

        [Fact]
        public async Task VerifyAsync_MissingSignature_RequirePolicy_IsRejected()
        {
            string packagePath = CreateUnsignedPackage();

            await Assert.ThrowsAsync<InvalidDataException>(() => VerifyAsync(packagePath, "require"));
        }

        private string CreateUnsignedPackage()
        {
            string packagePath = Path.Combine(_environmentSettingsHelper.CreateTemporaryFolder(), "Unsigned Package.nupkg");
            using (var archive = ZipFile.Open(packagePath, ZipArchiveMode.Create))
            {
                using var writer = new StreamWriter(archive.CreateEntry("TestPackage.nuspec").Open());
                writer.Write("<package><metadata><id>TestPackage</id><version>1.0.0</version><authors>Test</authors><description>Test package</description></metadata></package>");
            }

            return packagePath;
        }

        private Task VerifyAsync(string packagePath, string policy = "accept")
        {
            string directory = _environmentSettingsHelper.CreateTemporaryFolder();
            File.WriteAllText(
                Path.Combine(directory, "NuGet.config"),
                $"""
                <configuration>
                    <config>
                        <add key="signatureValidationMode" value="{policy}" />
                    </config>
                    <trustedSigners>
                        <clear />
                    </trustedSigners>
                </configuration>
                """);
            ISettings settings = global::NuGet.Configuration.Settings.LoadSpecificSettings(directory, "NuGet.config");
            return new DotnetNuGetPackageVerifier().VerifyAsync(
                packagePath,
                Repository.Factory.GetCoreV3(directory),
                settings,
                NullLogger.Instance,
                CancellationToken.None);
        }
    }
}
