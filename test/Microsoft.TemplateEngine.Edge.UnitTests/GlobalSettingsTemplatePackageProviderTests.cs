// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatePackage;
using Microsoft.TemplateEngine.Edge.BuiltInManagedProvider;
using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateEngine.Tests;
using Xunit;

namespace Microsoft.TemplateEngine.Edge.UnitTests
{
    public class GlobalSettingsTemplatePackageProviderTests : TestBase, IClassFixture<EnvironmentSettingsHelper>
    {
        private readonly EnvironmentSettingsHelper _environmentSettingsHelper;

        public GlobalSettingsTemplatePackageProviderTests(EnvironmentSettingsHelper environmentSettingsHelper)
        {
            _environmentSettingsHelper = environmentSettingsHelper;
        }

        [Fact]
        public async Task InstallAsync_OverlappingIdentity_UnmanagedPreinstalled_DoesNotOverwrite()
        {
            var engineEnvironmentSettings = _environmentSettingsHelper
               .CreateEnvironment(
               hostIdentifier: GetType().Name,
               virtualize: false,
               loadDefaultGenerator: true,
               additionalComponents: new[]
               {
                   // adds unmanaged TemplateA with the same identity
                    (typeof(ITemplatePackageProviderFactory), (IIdentifiedComponent)new MockTemplatePackageProviderFactory())
               });

            string templateBMountPath = GetTestTemplateLocation(Path.Combine("TemplateWithOverlappingIdentity", "TemplateB"));
            var globalSettingsProvider = new GlobalSettingsTemplatePackageProvider(new GlobalSettingsTemplatePackageProviderFactory(), engineEnvironmentSettings);
            var templateBInstallRequest = new InstallRequest(templateBMountPath);

            var installResults = await globalSettingsProvider.InstallAsync(new[] { templateBInstallRequest }, CancellationToken.None);

            Assert.NotNull(installResults[0]);
            Assert.Equal(InstallerErrorCode.DuplicatedIdentity, installResults[0].Error);
        }

        [Fact]
        public async Task InstallAsync_OverlappingIdentity_UnmanagedPreinstalled_OverwritesWithForce()
        {
            var engineEnvironmentSettings = _environmentSettingsHelper
                .CreateEnvironment(
                hostIdentifier: GetType().Name,
                virtualize: false,
                loadDefaultGenerator: true,
                additionalComponents: new[]
                {
                    // adds unmanaged TemplateA
                    (typeof(ITemplatePackageProviderFactory), (IIdentifiedComponent)new MockTemplatePackageProviderFactory())
                });

            string templateBMountPath = GetTestTemplateLocation(Path.Combine("TemplateWithOverlappingIdentity", "TemplateB"));
            var globalSettingsProvider = new GlobalSettingsTemplatePackageProvider(new GlobalSettingsTemplatePackageProviderFactory(), engineEnvironmentSettings);
            var templateBInstallRequest = new InstallRequest(templateBMountPath, force: true);

            var installResults = await globalSettingsProvider.InstallAsync(new[] { templateBInstallRequest }, CancellationToken.None);

            var installedTemplateB = installResults[0].TemplatePackage;
            Assert.NotNull(installedTemplateB);
            Assert.Equal(InstallerErrorCode.Success, installResults[0].Error);
            await globalSettingsProvider.UninstallAsync(new[] { installedTemplateB }, CancellationToken.None);
        }

        [Fact]
        public async Task InstallAsync_OverlappingIdentity_ManagedPreinstalled_DoesNotOverwrite()
        {
            var engineEnvironmentSettings = _environmentSettingsHelper
                .CreateEnvironment(hostIdentifier: GetType().Name, virtualize: false, loadDefaultGenerator: true);

            string templateAMountPath = GetTestTemplateLocation(Path.Combine("TemplateWithOverlappingIdentity", "TemplateA"));
            string templateBMountPath = GetTestTemplateLocation(Path.Combine("TemplateWithOverlappingIdentity", "TemplateB"));
            var globalSettingsProvider = new GlobalSettingsTemplatePackageProvider(new GlobalSettingsTemplatePackageProviderFactory(), engineEnvironmentSettings);

            var templateAInstallResults = await globalSettingsProvider.InstallAsync(new[] { new InstallRequest(templateAMountPath) }, CancellationToken.None);
            var templateBInstallResults = await globalSettingsProvider.InstallAsync(new[] { new InstallRequest(templateBMountPath) }, CancellationToken.None);

            Assert.NotNull(templateBInstallResults[0]);
            Assert.Equal(InstallerErrorCode.DuplicatedIdentity, templateBInstallResults[0].Error);

            var installedTemplateA = templateAInstallResults[0].TemplatePackage;
            Assert.NotNull(installedTemplateA);
            await globalSettingsProvider.UninstallAsync(new[] { installedTemplateA }, CancellationToken.None);
        }

        [Fact]
        public async Task InstallAsync_OverlappingIdentity_ManagedPreinstalled_DoesNotOverwriteWithForce()
        {
            var engineEnvironmentSettings = _environmentSettingsHelper
                .CreateEnvironment(hostIdentifier: GetType().Name, virtualize: false, loadDefaultGenerator: true);

            string templateAMountPath = GetTestTemplateLocation(Path.Combine("TemplateWithOverlappingIdentity", "TemplateA"));
            string templateBMountPath = GetTestTemplateLocation(Path.Combine("TemplateWithOverlappingIdentity", "TemplateB"));
            var globalSettingsProvider = new GlobalSettingsTemplatePackageProvider(new GlobalSettingsTemplatePackageProviderFactory(), engineEnvironmentSettings);

            var templateAInstallResults = await globalSettingsProvider.InstallAsync(new[] { new InstallRequest(templateAMountPath) }, CancellationToken.None);
            var templateBInstallResults = await globalSettingsProvider.InstallAsync(new[] { new InstallRequest(templateBMountPath, force: true) }, CancellationToken.None);

            Assert.NotNull(templateBInstallResults[0]);
            Assert.Equal(InstallerErrorCode.DuplicatedIdentity, templateBInstallResults[0].Error);

            var installedTemplateA = templateAInstallResults[0].TemplatePackage;
            Assert.NotNull(installedTemplateA);
            await globalSettingsProvider.UninstallAsync(new[] { installedTemplateA }, CancellationToken.None);
        }

        public class MockTemplatePackageProviderFactory : ITemplatePackageProviderFactory
        {
            public static readonly Guid FactoryId = Guid.Empty;

            public string DisplayName => "BuiltIn";

            public Guid Id => FactoryId;

            public ITemplatePackageProvider CreateProvider(IEngineEnvironmentSettings settings) => new MockTemplatePackageProvider();
        }

        public class MockTemplatePackageProvider : ITemplatePackageProvider
        {
            public ITemplatePackageProviderFactory Factory => new MockTemplatePackageProviderFactory();

#pragma warning disable CS0067

            public event Action? TemplatePackagesChanged;

#pragma warning restore CS0067

            public Task<IReadOnlyList<ITemplatePackage>> GetAllTemplatePackagesAsync(CancellationToken cancellationToken)
            {
                IReadOnlyList<ITemplatePackage> list = new List<ITemplatePackage>
                {
                     new TemplatePackage(this, GetTestTemplateLocation(Path.Combine("TemplateWithOverlappingIdentity", "TemplateA")), DateTime.MinValue)
                };

                return Task.FromResult(list);
            }

            public Task<IReadOnlyList<CheckUpdateResult>> GetLatestVersionsAsync(IEnumerable<IManagedTemplatePackage> managedSources, CancellationToken cancellationToken) => throw new NotImplementedException();

            public Task<IReadOnlyList<InstallResult>> InstallAsync(IEnumerable<InstallRequest> installRequests, CancellationToken cancellationToken) => throw new NotImplementedException();

            public Task<IReadOnlyList<UninstallResult>> UninstallAsync(IEnumerable<IManagedTemplatePackage> managedSources, CancellationToken cancellationToken) => throw new NotImplementedException();

            public Task<IReadOnlyList<UpdateResult>> UpdateAsync(IEnumerable<UpdateRequest> updateRequests, CancellationToken cancellationToken) => throw new NotImplementedException();
        }
    }
}
