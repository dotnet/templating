// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using Microsoft.TemplateEngine.IDE.IntegrationTests.Utils;
using Xunit;

namespace Microsoft.TemplateEngine.IDE.IntegrationTests
{
    public class TemplatePackagesTests : IClassFixture<PackageManager>
    {
        private PackageManager _packageManager;
        public TemplatePackagesTests(PackageManager packageManager)
        {
            _packageManager = packageManager;
        }

        [Fact]
        internal async Task CanInstallLocalNuGetPackage()
        {
            Bootstrapper bootstrapper = BootstrapperFactory.GetBootstrapper();
            string packageLocation = _packageManager.PackProjectTemplatesNuGetPackage("microsoft.dotnet.common.projecttemplates.5.0");

            InstallRequest installRequest = new InstallRequest
            {
                Identifier = Path.GetFullPath(packageLocation)
            };

            IReadOnlyList<InstallResult> result = await bootstrapper.InstallAsync(new[] { installRequest }, InstallationScope.Global, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(1, result.Count);
            Assert.True(result.First().Success);
            Assert.Equal(InstallerErrorCode.Success, result.First().Error);
            result.First().ErrorMessage.Should().BeNullOrEmpty();
            Assert.Equal(installRequest, result.First().InstallRequest);

            IManagedTemplatesSource source = result.First().Source;
            Assert.Equal("Microsoft.DotNet.Common.ProjectTemplates.5.0", source.Identifier);
            Assert.Equal("Global Settings", source.Provider.Factory.Name);
            Assert.Equal("NuGet", source.Installer.Name);
            Assert.Equal("Microsoft", source.GetDisplayDetails()["Author"]);
            source.Version.Should().NotBeNullOrEmpty();

            IReadOnlyList<IManagedTemplatesSource> managedTemplatesPackages = await bootstrapper.GetManagedTemplatesSources(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(1, managedTemplatesPackages.Count);
            managedTemplatesPackages.First().Should().BeEquivalentTo(source);

            IReadOnlyList<ITemplatesSource> templatePackages = await bootstrapper.GetTemplatesSources(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(1, templatePackages.Count);
            Assert.IsAssignableFrom<IManagedTemplatesSource>(templatePackages.First());
            templatePackages.First().Should().BeEquivalentTo((ITemplatesSource)source);
        }

        [Fact]
        internal async Task CanInstallRemoteNuGetPackage()
        {
            Bootstrapper bootstrapper = BootstrapperFactory.GetBootstrapper();
            InstallRequest installRequest = new InstallRequest
            {
                Identifier = "Microsoft.DotNet.Common.ProjectTemplates.5.0",
                Version = "5.0.0",
                Details = new Dictionary<string, string>
                {
                    { "NuGetSource", "https://api.nuget.org/v3/index.json" }
                }
            };

            IReadOnlyList<InstallResult> result = await bootstrapper.InstallAsync(new[] { installRequest }, InstallationScope.Global, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(1, result.Count);
            Assert.True(result.First().Success);
            Assert.Equal(InstallerErrorCode.Success, result.First().Error);
            Assert.True(string.IsNullOrEmpty(result.First().ErrorMessage));
            Assert.Equal(installRequest, result.First().InstallRequest);

            IManagedTemplatesSource source = result.First().Source;
            Assert.Equal("Microsoft.DotNet.Common.ProjectTemplates.5.0", source.Identifier);
            Assert.Equal("Global Settings", source.Provider.Factory.Name);
            Assert.Equal("NuGet", source.Installer.Name);
            Assert.Equal("Microsoft", source.GetDisplayDetails()["Author"]);
            //TODO: enable when UX improvements are merged
            //Assert.Equal("https://api.nuget.org/v3/index.json", source.GetDisplayDetails()["NuGetSource"]);
            Assert.Equal("5.0.0", source.Version);

            IReadOnlyList<IManagedTemplatesSource> managedTemplatesPackages = await bootstrapper.GetManagedTemplatesSources(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(1, managedTemplatesPackages.Count);
            managedTemplatesPackages.First().Should().BeEquivalentTo(source);

            IReadOnlyList<ITemplatesSource> templatePackages = await bootstrapper.GetTemplatesSources(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(1, templatePackages.Count);
            Assert.IsAssignableFrom<IManagedTemplatesSource>(templatePackages.First());
            templatePackages.First().Should().BeEquivalentTo((ITemplatesSource)source);
        }

        [Fact]
        internal async Task CanInstallTemplateFromFolder()
        {
            Bootstrapper bootstrapper = BootstrapperFactory.GetBootstrapper();
            string templateLocation = TestHelper.GetTestTemplateLocation("TemplateWithSourceName");

            InstallRequest installRequest = new InstallRequest
            {
                Identifier = Path.GetFullPath(templateLocation)
            };

            IReadOnlyList<InstallResult> result = await bootstrapper.InstallAsync(new[] { installRequest }, InstallationScope.Global, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(1, result.Count);
            Assert.True(result.First().Success);
            Assert.Equal(InstallerErrorCode.Success, result.First().Error);
            result.First().ErrorMessage.Should().BeNullOrEmpty();
            Assert.Equal(installRequest, result.First().InstallRequest);

            IManagedTemplatesSource source = result.First().Source;
            Assert.Equal(Path.GetFullPath(templateLocation), source.Identifier);
            Assert.Equal("Global Settings", source.Provider.Factory.Name);
            Assert.Equal("Folder", source.Installer.Name);
            source.Version.Should().BeNullOrEmpty();

            IReadOnlyList<IManagedTemplatesSource> managedTemplatesPackages = await bootstrapper.GetManagedTemplatesSources(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(1, managedTemplatesPackages.Count);
            managedTemplatesPackages.First().Should().BeEquivalentTo(source);

            IReadOnlyList<ITemplatesSource> templatePackages = await bootstrapper.GetTemplatesSources(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(1, templatePackages.Count);
            Assert.IsAssignableFrom<IManagedTemplatesSource>(templatePackages.First());
            templatePackages.First().Should().BeEquivalentTo((ITemplatesSource)source);
        }
    }
}
