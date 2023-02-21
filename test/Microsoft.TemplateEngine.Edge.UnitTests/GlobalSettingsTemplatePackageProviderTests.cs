// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FakeItEasy;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Abstractions.TemplatePackage;
using Microsoft.TemplateEngine.Edge.BuiltInManagedProvider;
using Microsoft.TemplateEngine.Edge.Installers.Folder;
using Microsoft.TemplateEngine.Mocks;
using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateEngine.Tests;
using Xunit;

namespace Microsoft.TemplateEngine.Edge.UnitTests
{
    public class GlobalSettingsTemplatePackageProviderTests : TestBase, IClassFixture<EnvironmentSettingsHelper>
    {
        private readonly EnvironmentSettingsHelper _environmentSettingsHelper;

        public GlobalSettingsTemplatePackageProviderTests(EnvironmentSettingsHelper environmentSettingsHelper) => _environmentSettingsHelper = environmentSettingsHelper;

        [Fact]
        public async Task InstallAsync_OverlappingIdentity_UnmanagedPreinstalled_DoesNotOverwrite()
        {
            var engineEnvironmentSettings = _environmentSettingsHelper
               .CreateEnvironment(
               hostIdentifier: GetType().Name,
               virtualize: true,
               loadDefaultGenerator: false,
               additionalComponents: new[]
               {
                   (typeof(IInstallerFactory), GetInstallerFactoryMock()),
                   (typeof(IMountPointFactory), new MountPointFactoryMock()),
                   // adds unmanaged template TemplateA
                   (typeof(ITemplatePackageProviderFactory), GetTemplatePackageProviderFactoryMock()),
                   (typeof(IGenerator), (IIdentifiedComponent)GetGeneratorMock())
               });

            string templateBMountPath = "TemplateBPath";
            var globalSettingsProvider = new GlobalSettingsTemplatePackageProvider(new GlobalSettingsTemplatePackageProviderFactory(), engineEnvironmentSettings);
            var templateBInstallRequest = new InstallRequest(templateBMountPath, "InstallerMock");

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
              virtualize: true,
              loadDefaultGenerator: false,
              additionalComponents: new[]
              {
                   (typeof(IInstallerFactory), GetInstallerFactoryMock()),
                   (typeof(IMountPointFactory), new MountPointFactoryMock()),
                   // adds unmanaged template TemplateA
                   (typeof(ITemplatePackageProviderFactory), GetTemplatePackageProviderFactoryMock()),
                   (typeof(IGenerator), (IIdentifiedComponent)GetGeneratorMock())
              });

            string templateBMountPath = "TemplateBPath";
            var globalSettingsProvider = new GlobalSettingsTemplatePackageProvider(new GlobalSettingsTemplatePackageProviderFactory(), engineEnvironmentSettings);
            var templateBInstallRequest = new InstallRequest(templateBMountPath, "InstallerMock", force: true);

            var installResults = await globalSettingsProvider.InstallAsync(new[] { templateBInstallRequest }, CancellationToken.None);

            Assert.NotNull(installResults);
            Assert.Equal(InstallerErrorCode.Success, installResults[0].Error);
        }

        [Fact]
        public async Task InstallAsync_OverlappingIdentity_ManagedPreinstalled_DoesNotOverwrite()
        {
            var engineEnvironmentSettings = _environmentSettingsHelper
              .CreateEnvironment(
              hostIdentifier: GetType().Name,
              virtualize: true,
              loadDefaultGenerator: false,
              additionalComponents: new[]
              {
                   (typeof(IInstallerFactory), GetInstallerFactoryMock()),
                   (typeof(IMountPointFactory), new MountPointFactoryMock()),
                   // adds managed template TemplateA
                   (typeof(ITemplatePackageProviderFactory), GetTemplatePackageProviderFactoryMock(shouldMockManagedTemplate: true)),
                   (typeof(IGenerator), (IIdentifiedComponent)GetGeneratorMock())
              });

            string templateBMountPath = "TemplateBPath";
            var globalSettingsProvider = new GlobalSettingsTemplatePackageProvider(new GlobalSettingsTemplatePackageProviderFactory(), engineEnvironmentSettings);
            var templateBInstallRequest = new InstallRequest(templateBMountPath, "InstallerMock");

            var installResults = await globalSettingsProvider.InstallAsync(new[] { templateBInstallRequest }, CancellationToken.None);

            Assert.NotNull(installResults);
            Assert.Equal(InstallerErrorCode.DuplicatedIdentity, installResults[0].Error);
        }

        [Fact]
        public async Task InstallAsync_OverlappingIdentity_ManagedPreinstalled_DoesNotOverwriteWithForce()
        {
            var engineEnvironmentSettings = _environmentSettingsHelper
             .CreateEnvironment(
             hostIdentifier: GetType().Name,
             virtualize: true,
             loadDefaultGenerator: false,
             additionalComponents: new[]
             {
                   (typeof(IInstallerFactory), GetInstallerFactoryMock()),
                   (typeof(IMountPointFactory), new MountPointFactoryMock()),
                   // adds managed template TemplateA
                   (typeof(ITemplatePackageProviderFactory), GetTemplatePackageProviderFactoryMock(shouldMockManagedTemplate: true)),
                   (typeof(IGenerator), (IIdentifiedComponent)GetGeneratorMock())
             });

            string templateBMountPath = "TemplateBPath";
            var globalSettingsProvider = new GlobalSettingsTemplatePackageProvider(new GlobalSettingsTemplatePackageProviderFactory(), engineEnvironmentSettings);
            var templateBInstallRequest = new InstallRequest(templateBMountPath, "InstallerMock", force: true);

            var installResults = await globalSettingsProvider.InstallAsync(new[] { templateBInstallRequest }, CancellationToken.None);

            Assert.NotNull(installResults);
            Assert.Equal(InstallerErrorCode.DuplicatedIdentity, installResults[0].Error);
        }

        public IInstallerFactory GetInstallerFactoryMock()
        {
            var installerFactoryMock = A.Fake<IInstallerFactory>();

            A.CallTo(() => installerFactoryMock.Name).Returns("InstallerMock");

            A.CallTo(() => installerFactoryMock.CreateInstaller(A<IEngineEnvironmentSettings>._, A<string>._))
                .ReturnsLazily((IEngineEnvironmentSettings settings, string path) => GetInstallerMock(settings, installerFactoryMock));

            return installerFactoryMock;
        }

        public interface IManagedInstallerMock : IInstaller, ISerializableInstaller { }

        internal IManagedInstallerMock GetInstallerMock(IEngineEnvironmentSettings settings, IInstallerFactory factory)
        {
            var installerMock = A.Fake<IManagedInstallerMock>();

            A.CallTo(() => installerMock.CanInstallAsync(A<InstallRequest>._, A<CancellationToken>._))
                .Returns(true);

            A.CallTo(() => installerMock.Factory).Returns(factory);

            A.CallTo(() => installerMock.InstallAsync(A<InstallRequest>._, A<IManagedTemplatePackageProvider>._, A<CancellationToken>._))
                .ReturnsLazily(
                (InstallRequest installRequest, IManagedTemplatePackageProvider provider, CancellationToken cancellationToken) =>
                Task.FromResult(InstallResult.CreateSuccess(installRequest, new FolderManagedTemplatePackage(
                        settings,
                        installerMock,
                        new MockManagedTemplatePackageProvider(),
                        "TemplateBPath",
                        DateTime.MinValue))));

            A.CallTo(() => installerMock.Serialize(A<IManagedTemplatePackage>._))
               .Returns(new TemplatePackageData(Guid.Empty, "TemplateBPath", DateTime.Now, new Dictionary<string, string>()));

            A.CallTo(() => installerMock.UninstallAsync(A<IManagedTemplatePackage>._, A<IManagedTemplatePackageProvider>._, A<CancellationToken>._))
               .ReturnsLazily((
                IManagedTemplatePackage templatePackage,
                IManagedTemplatePackageProvider provider,
                CancellationToken cancellationToken) => Task.FromResult(UninstallResult.CreateSuccess(templatePackage)));

            return installerMock;
        }

        internal ITemplatePackageProviderFactory GetTemplatePackageProviderFactoryMock(bool shouldMockManagedTemplate = false)
        {
            var templatePackageProviderFactoryMock = A.Fake<ITemplatePackageProviderFactory>();

            A.CallTo(() => templatePackageProviderFactoryMock.CreateProvider(A<IEngineEnvironmentSettings>._))
                .ReturnsLazily((IEngineEnvironmentSettings settings) => GetTemplatePackageProviderMock(
                    templatePackageProviderFactoryMock, settings, shouldMockManagedTemplate));

            return templatePackageProviderFactoryMock;
        }

        internal ITemplatePackageProvider GetTemplatePackageProviderMock(
            ITemplatePackageProviderFactory factory,
            IEngineEnvironmentSettings settings,
            bool shouldMockManagedTemplate)
        {
            var installerMock = A.Fake<IManagedInstallerMock>();

            var templatePackageProviderMock = A.Fake<ITemplatePackageProvider>();
            IReadOnlyList<ITemplatePackage> list = new List<ITemplatePackage>
            {
                shouldMockManagedTemplate
                    ? new FolderManagedTemplatePackage(settings, installerMock, A.Fake<IManagedTemplatePackageProvider>(), "TemplateAPath", DateTime.MinValue)
                    : new TemplatePackage(templatePackageProviderMock, "TemplateAPath", DateTime.MinValue)
            };

            A.CallTo(() => templatePackageProviderMock.GetAllTemplatePackagesAsync(A<CancellationToken>._)).Returns(list);

            A.CallTo(() => templatePackageProviderMock.Factory).Returns(factory);

            return templatePackageProviderMock;
        }

        internal class MountPointFactoryMock : IMountPointFactory
        {
            public Guid Id => Guid.Empty;

            public bool TryMount(IEngineEnvironmentSettings environmentSettings, IMountPoint? parent, string mountPointUri, out IMountPoint? mountPoint)
            {
                mountPoint = new MockMountPoint(environmentSettings, mountPointUri);

                return true;
            }
        }

        internal IGenerator GetGeneratorMock()
        {
            var generatorMock = A.Fake<IGenerator>();
            IList<ILocalizationLocator> localizations = new List<ILocalizationLocator>();

            var templates = new List<ITemplate>();
#pragma warning disable CS8604 // Possible null reference argument.
            A.CallTo(() => generatorMock.GetTemplatesAndLangpacksFromDir(A<IMountPoint>._, out localizations))
                .ReturnsLazily(call =>
                {
                    var template = new MockTemplate(generatorMock, call.GetArgument<IMountPoint>(0));
                    template.MountPointUriToIdentityMapMock.Add("TemplateAPath", "OVERLAPPINGIDENTITY");
                    template.MountPointUriToIdentityMapMock.Add("TemplateBPath", "OVERLAPPINGIDENTITY");

                    return new List<ITemplate>() { template };
                });
#pragma warning restore CS8604 // Possible null reference argument.

            return generatorMock;
        }
    }
}
