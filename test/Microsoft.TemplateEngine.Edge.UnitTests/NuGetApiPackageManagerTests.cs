// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Installers.NuGet;
using Microsoft.TemplateEngine.Edge.UnitTests.Mocks;
using Microsoft.TemplateEngine.TestHelper;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Xunit;
using NuGetSettings = NuGet.Configuration.Settings;

namespace Microsoft.TemplateEngine.Edge.UnitTests
{
    public class NuGetApiPackageManagerTests : IClassFixture<EnvironmentSettingsHelper>
    {
        private readonly EnvironmentSettingsHelper _environmentSettingsHelper;
        private readonly IList<string> _additionalSources = new[] { "https://api.nuget.org/v3/index.json" };

        public NuGetApiPackageManagerTests(EnvironmentSettingsHelper environmentSettingsHelper)
        {
            _environmentSettingsHelper = environmentSettingsHelper;
        }

        [Theory]
        [InlineData("1.0.0")]
        [InlineData(null)]
        [InlineData("*")]
        [InlineData("1.*")]
        [InlineData("*-*")]
        public async Task DownloadPackage_MismatchedMetadataId_ThrowsPackageNotFound(string? version)
        {
            const string identifier = "Requested.Package";
            using var feed = new MockNuGetFeed("Other.Package");
            string source = feed.BaseAddress + "index.json";
            string installPath = _environmentSettingsHelper.CreateTemporaryFolder();
            var settings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);
            var manager = new NuGetApiPackageManager(settings);

            var exception = await Assert.ThrowsAsync<PackageNotFoundException>(() => manager.DownloadPackageAsync(
                installPath, identifier, version, additionalSources: new[] { source }, force: true));

            exception.PackageIdentifier.Should().Be(identifier);
            NuGetVersion.TryParse(version, out NuGetVersion? expectedVersion);
            Assert.Equal(expectedVersion, exception.PackageVersion);
            exception.SourcesList.Should().Equal(source);
            Directory.GetFiles(installPath).Should().BeEmpty();
            feed.Requests.Should().NotContain(path => path.StartsWith("/flatcontainer/", StringComparison.Ordinal));
        }

        [Fact]
        public async Task GetLatestVersion_MismatchedMetadataId_ThrowsPackageNotFound()
        {
            const string identifier = "Requested.Package";
            using var feed = new MockNuGetFeed("Other.Package");
            string source = feed.BaseAddress + "index.json";
            var settings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);
            var manager = new NuGetApiPackageManager(settings);

            var exception = await Assert.ThrowsAsync<PackageNotFoundException>(() => manager.GetLatestVersionAsync(
                identifier, additionalSource: source));

            exception.PackageIdentifier.Should().Be(identifier);
            exception.PackageVersion.Should().BeNull();
            exception.SourcesList.Should().Equal(source);
        }

        [Fact]
        public async Task DownloadPackage_Latest()
        {
            string installPath = _environmentSettingsHelper.CreateTemporaryFolder();
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);

            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            var result = await packageManager.DownloadPackageAsync(
                installPath,
                "Microsoft.DotNet.Common.ProjectTemplates.5.0",
                // use a different source for checking specific nuget metadata
                additionalSources: new[] { "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json" });

            result.Author.Should().Be("Microsoft");
            result.FullPath.Should().ContainAll(installPath, "Microsoft.DotNet.Common.ProjectTemplates.5.0");
            Assert.True(File.Exists(result.FullPath));
            result.PackageIdentifier.Should().Be("Microsoft.DotNet.Common.ProjectTemplates.5.0");
            result.Owners.Should().Be(string.Empty);
            result.Reserved.Should().BeFalse();
            result.PackageVersion.Should().NotBeNullOrEmpty();
            result.NuGetSource.Should().NotBeNullOrEmpty();
            Directory.EnumerateFiles(installPath, "*.tmp").Should().BeEmpty();
        }

        [Fact]
        public async Task DownloadPackage_PackageValidationFailure_RemovesTemporaryPackage()
        {
            string installPath = _environmentSettingsHelper.CreateTemporaryFolder();
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);

            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(
                engineEnvironmentSettings,
                new FailingNuGetPackageVerifier());

            var exception = await Assert.ThrowsAsync<DownloadException>(() => packageManager.DownloadPackageAsync(
                installPath,
                "Microsoft.DotNet.Common.ProjectTemplates.5.0",
                "5.0.0",
                additionalSources: new[] { "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json" }));

            var validationException = Assert.IsType<InvalidDataException>(exception.InnerException);
            validationException.Message.Should().Be("Package validation failed.");

            Directory.EnumerateFiles(installPath).Should().BeEmpty();
        }

        [Fact]
        public async Task DownloadPackage_LatestFromNugetOrgFeed()
        {
            string installPath = _environmentSettingsHelper.CreateTemporaryFolder();
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);

            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);

            var result = await packageManager.DownloadPackageAsync(
                installPath,
                "Microsoft.DotNet.Common.ProjectTemplates.5.0",
                // add the source for getting ownership info
                additionalSources: _additionalSources);

            result.Author.Should().Be("Microsoft");
            result.FullPath.Should().ContainAll(installPath, "Microsoft.DotNet.Common.ProjectTemplates.5.0");
            Assert.True(File.Exists(result.FullPath));
            result.PackageIdentifier.Should().Be("Microsoft.DotNet.Common.ProjectTemplates.5.0");
            result.Owners.Should().Be("dotnetframework, Microsoft");
            result.Reserved.Should().BeTrue();
            result.PackageVersion.Should().NotBeNullOrEmpty();
            result.NuGetSource.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task DownloadPackage_SpecificVersion()
        {
            string installPath = _environmentSettingsHelper.CreateTemporaryFolder();
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);

            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            var result = await packageManager.DownloadPackageAsync(
                installPath,
                "Microsoft.DotNet.Common.ProjectTemplates.5.0",
                "5.0.0",
                additionalSources: _additionalSources);

            result.Author.Should().Be("Microsoft");
            result.FullPath.Should().ContainAll(installPath, "Microsoft.DotNet.Common.ProjectTemplates.5.0", "5.0.0");
            Assert.True(File.Exists(result.FullPath));
            result.PackageIdentifier.Should().Be("Microsoft.DotNet.Common.ProjectTemplates.5.0");
            result.PackageVersion.Should().Be("5.0.0");
            result.NuGetSource.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task DownloadPackage_UnknownPackage()
        {
            string installPath = _environmentSettingsHelper.CreateTemporaryFolder();
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);

            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            var exception = await Assert.ThrowsAsync<PackageNotFoundException>(() => packageManager.DownloadPackageAsync(
                installPath, "Microsoft.DotNet.NotCommon.ProjectTemplates.5.0", "5.0.0", additionalSources: _additionalSources));

            exception.PackageIdentifier.Should().Be("Microsoft.DotNet.NotCommon.ProjectTemplates.5.0");
            exception.PackageVersion.Should().NotBeNull();
            exception.PackageVersion!.ToString().Should().Be("5.0.0");
            exception.Message.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task DownloadPackage_InvalidPath()
        {
            string installPath = ":/?";
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);

            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            var exception = await Assert.ThrowsAsync<DownloadException>(() => packageManager.DownloadPackageAsync(
                installPath, "Microsoft.DotNet.Common.ProjectTemplates.5.0", "5.0.0", additionalSources: _additionalSources));

            exception.PackageIdentifier.Should().Be("Microsoft.DotNet.Common.ProjectTemplates.5.0");
            exception.PackageVersion.ToString().Should().Be("5.0.0");
            exception.Message.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task DownloadPackage_CannotOverwritePackage()
        {
            string installPath = _environmentSettingsHelper.CreateTemporaryFolder();
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);

            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            await packageManager.DownloadPackageAsync(installPath, "Microsoft.DotNet.Common.ProjectTemplates.5.0", "5.0.0", additionalSources: _additionalSources);
            var exception = await Assert.ThrowsAsync<DownloadException>(() => packageManager.DownloadPackageAsync(
                installPath, "Microsoft.DotNet.Common.ProjectTemplates.5.0", "5.0.0", additionalSources: _additionalSources));

            exception.PackageIdentifier.Should().Be("Microsoft.DotNet.Common.ProjectTemplates.5.0");
            exception.PackageVersion.ToString().Should().Be("5.0.0");
            exception.Message.Should().NotBeNullOrEmpty();
        }

        [Fact]
        internal async Task DownloadPackage_HasVulnerabilities()
        {
            string installPath = _environmentSettingsHelper.CreateTemporaryFolder();
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);

            // Getting this version of the package as it has known vulnerabilities
            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);

            var exception = await Assert.ThrowsAsync<VulnerablePackageException>(() => packageManager.DownloadPackageAsync(
                installPath,
                "System.Text.Json",
                "8.0.4",
                // add the source for getting vulnerability info
                additionalSources: _additionalSources));

            exception.PackageIdentifier.Should().Be("System.Text.Json");
            exception.PackageVersion.Should().Be("8.0.4");
            exception.Vulnerabilities.Should().NotBeNullOrEmpty();
        }

        [Fact]
        internal async Task DownloadPackage_HasVulnerabilitiesForce()
        {
            string installPath = _environmentSettingsHelper.CreateTemporaryFolder();
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);

            // Getting this version of the package as it has known vulnerabilities
            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);

            var result = await packageManager.DownloadPackageAsync(
                installPath,
                "System.Text.Json",
                "8.0.4",
                // add the source for getting vulnerability info
                additionalSources: _additionalSources,
                force: true);

            result.PackageIdentifier.Should().Be("System.Text.Json");
            result.Author.Should().Be("Microsoft");
            result.PackageVersion.Should().Be("8.0.4");
            Assert.True(File.Exists(result.FullPath));
            result.PackageVulnerabilities.Should().NotBeNullOrEmpty();
            result.NuGetSource.Should().Be(_additionalSources[0]);
        }

        [Fact]
        public async Task GetLatestVersion_Success()
        {
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);

            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            (string latestVersion, bool isLatestVersion, _) = await packageManager.GetLatestVersionAsync("Microsoft.DotNet.Common.ProjectTemplates.5.0", additionalSource: _additionalSources.FirstOrDefault());

            latestVersion.Should().NotBeNullOrEmpty();
            isLatestVersion.Should().BeFalse();
        }

        [Fact]
        public async Task GetLatestVersion_SpecificVersion()
        {
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);

            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            (string latestVersion, bool isLatestVersion, _) = await packageManager.GetLatestVersionAsync(
                "Microsoft.DotNet.Common.ProjectTemplates.5.0", "5.0.0", additionalSource: _additionalSources.First());

            latestVersion.Should().NotBe("5.0.0");
            isLatestVersion.Should().BeFalse();
        }

        [Fact]
        public async Task GetLatestVersion_UnknownPackage()
        {
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);

            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            var exception = await Assert.ThrowsAsync<PackageNotFoundException>(() => packageManager.GetLatestVersionAsync(
                "Microsoft.DotNet.NotCommon.ProjectTemplates.5.0", "5.0.0", additionalSource: _additionalSources.FirstOrDefault()));

            exception.PackageIdentifier.Should().Be("Microsoft.DotNet.NotCommon.ProjectTemplates.5.0");
            exception.Message.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void LoadNuGetSources_PackageSourceMappingEnabled_ReturnsMappedSources()
        {
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);
            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            ISettings settings = CreateSettingsWithPackageSourceMapping();

            IEnumerable<PackageSource> sources = packageManager.LoadNuGetSources(
                "Microsoft.TemplateEngine.TestTemplates",
                Array.Empty<string>(),
                settings);

            sources.Should().ContainSingle()
                .Which.Name.Should().Be("template-feed");
        }

        [Fact]
        public void LoadNuGetSources_ExactPackageSourceMapping_ReturnsMappedSource()
        {
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);
            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            ISettings settings = CreateSettingsWithPackageSourceMapping();

            IEnumerable<PackageSource> sources = packageManager.LoadNuGetSources(
                "Exact.Templates",
                Array.Empty<string>(),
                settings);

            sources.Should().ContainSingle()
                .Which.Name.Should().Be("other-feed");
        }

        [Fact]
        public void LoadNuGetSources_PackageMappedToMultipleSources_ReturnsAllMappedSources()
        {
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);
            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            ISettings settings = CreateSettingsWithPackageSourceMapping();

            IEnumerable<PackageSource> sources = packageManager.LoadNuGetSources(
                "Shared.Templates",
                Array.Empty<string>(),
                settings);

            sources.Select(source => source.Name).Should().BeEquivalentTo("template-feed", "other-feed");
        }

        [Fact]
        public void LoadNuGetSources_PackageSourceMappingEnabledAndPackageNotMapped_Throws()
        {
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);
            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            ISettings settings = CreateSettingsWithPackageSourceMapping();

            Action action = () => packageManager.LoadNuGetSources(
                "Contoso.Templates",
                Array.Empty<string>(),
                settings);

            PackageNotFoundException exception = action.Should().Throw<PackageNotFoundException>().Which;
            exception.PackageSourceMappingMatchNotFound.Should().BeTrue();
        }

        [Fact]
        public void LoadNuGetSources_MappedSourceDisabled_Throws()
        {
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);
            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            ISettings settings = CreateSettingsWithPackageSourceMapping(disableTemplateSource: true);

            Action action = () => packageManager.LoadNuGetSources(
                "Microsoft.TemplateEngine.TestTemplates",
                Array.Empty<string>(),
                settings);

            PackageNotFoundException exception = action.Should().Throw<PackageNotFoundException>().Which;
            exception.PackageSourceMappingMatchNotFound.Should().BeTrue();
        }

        [Fact]
        public void LoadNuGetSources_PackageSourceMappingDisabled_ReturnsAllSources()
        {
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);
            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            ISettings settings = CreateSettingsWithPackageSourceMapping(includePackageSourceMapping: false);

            IEnumerable<PackageSource> sources = packageManager.LoadNuGetSources(
                "Contoso.Templates",
                Array.Empty<string>(),
                settings);

            sources.Select(source => source.Name).Should().BeEquivalentTo("template-feed", "other-feed");
        }

        [Theory]
        [InlineData("https://additional.test/v3/index.json")]
        [InlineData("https://templates.test/v3/index.json")]
        public void LoadNuGetSources_PackageSourceMappingEnabledAndAdditionalSourceSpecified_Throws(string additionalSource)
        {
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);
            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            ISettings settings = CreateSettingsWithPackageSourceMapping();

            Action action = () => packageManager.LoadNuGetSources(
                "Contoso.Templates",
                new[] { additionalSource },
                settings);

            action.Should().Throw<InvalidNuGetSourceException>();
        }

        [Fact]
        public void LoadNuGetSources_PackageSourceMappingEnabledAndNoSourcesEnabled_ThrowsInvalidNuGetSourceException()
        {
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);
            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            ISettings settings = CreateSettingsWithPackageSourceMapping(
                disableAllSources: true);

            Action action = () => packageManager.LoadNuGetSources(
                "Microsoft.TemplateEngine.TestTemplates",
                Array.Empty<string>(),
                settings);

            action.Should().Throw<InvalidNuGetSourceException>();
        }

        [Fact]
        public void RemoveInsecurePackages_AllInsecure()
        {
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);

            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            List<PackageSource> allPackages = new List<PackageSource>()
            {
                new PackageSource("http://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json"),
                new PackageSource("http://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json"),
                new PackageSource("http://pkgs.dev.azure.com/dnceng/public/_packaging/nuget-build/nuget/v3/index.json"),
                new PackageSource("http://insecure-feed.org")
            };
            var securePackages = packageManager.RemoveInsecurePackages(allPackages);

            securePackages.Should().BeEmpty();
        }

        [Fact]
        public void RemoveInsecurePackages_AllSecure()
        {
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);

            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            List<PackageSource> allPackages = new List<PackageSource>()
            {
                new PackageSource("https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json"),
                new PackageSource("https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json"),
                new PackageSource("https://pkgs.dev.azure.com/dnceng/public/_packaging/nuget-build/nuget/v3/index.json")
            };
            var securePackages = packageManager.RemoveInsecurePackages(allPackages);

            securePackages.Should().NotBeEmpty();
            Assert.Equal(allPackages, securePackages);
        }

        [Fact]
        public void RemoveInsecurePackages_Mixed()
        {
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true);

            NuGetApiPackageManager packageManager = new NuGetApiPackageManager(engineEnvironmentSettings);
            List<PackageSource> allPackages = new List<PackageSource>()
            {
                new PackageSource("https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json"),
                new PackageSource("https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json"),
                new PackageSource("http://pkgs.dev.azure.com/dnceng/public/_packaging/nuget-build/nuget/v3/index.json"),
                new PackageSource("http://insecure-feed.org")
            };
            var securePackages = packageManager.RemoveInsecurePackages(allPackages);

            var expectedOutcome = new List<PackageSource>()
            {
                new PackageSource("https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json"),
                new PackageSource("https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json")
            };

            securePackages.Should().NotBeEmpty();
            securePackages.Should().Equal(expectedOutcome);
        }

        private ISettings CreateSettingsWithPackageSourceMapping(
            bool disableTemplateSource = false,
            bool disableAllSources = false,
            bool includePackageSourceMapping = true)
        {
            string settingsDirectory = _environmentSettingsHelper.CreateTemporaryFolder();
            string disabledSources = disableAllSources
                ? """
                  <disabledPackageSources>
                    <add key="template-feed" value="true" />
                    <add key="other-feed" value="true" />
                  </disabledPackageSources>
                  """
                : disableTemplateSource
                ? """
                  <disabledPackageSources>
                    <add key="template-feed" value="true" />
                  </disabledPackageSources>
                  """
                : string.Empty;
            string packageSourceMapping = includePackageSourceMapping
                ? """
                  <packageSourceMapping>
                    <packageSource key="template-feed">
                      <package pattern="Microsoft.TemplateEngine.*" />
                      <package pattern="Shared.Templates" />
                    </packageSource>
                    <packageSource key="other-feed">
                      <package pattern="Other.*" />
                      <package pattern="Exact.Templates" />
                      <package pattern="Shared.Templates" />
                    </packageSource>
                  </packageSourceMapping>
                  """
                : string.Empty;
            File.WriteAllText(
                Path.Combine(settingsDirectory, "NuGet.config"),
                $"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <packageSources>
                    <clear />
                    <add key="template-feed" value="https://templates.test/v3/index.json" />
                    <add key="other-feed" value="https://other.test/v3/index.json" />
                  </packageSources>
                {disabledSources}
                {packageSourceMapping}
                </configuration>
                """);
            return NuGetSettings.LoadSpecificSettings(settingsDirectory, "NuGet.config");
        }

        private sealed class FailingNuGetPackageVerifier : INuGetPackageVerifier
        {
            public bool IsEnabled => true;

            public Task VerifyAsync(string packagePath, SourceRepository repository, ISettings settings, ILogger logger, CancellationToken cancellationToken)
            {
                throw new InvalidDataException("Package validation failed.");
            }
        }
    }
}
