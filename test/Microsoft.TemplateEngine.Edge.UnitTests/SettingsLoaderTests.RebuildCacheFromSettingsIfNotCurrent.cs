using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateEngine.Utils;
using Xunit;

namespace Microsoft.TemplateEngine.Edge.UnitTests
{
    public class SettingsLoaderTests : IDisposable
    {
        class FakeFactory : ITemplatesSourcesProviderFactory
        {
            public string Name => nameof(FakeFactory);

            public Guid Id { get; } = new Guid("{61CFA828-97B6-44EB-A44D-0AE673D6DF52}");

            public ITemplatesSourcesProvider CreateProvider(IEngineEnvironmentSettings settings)
            {
                var defaultTemplatesSourceProvider = new DefaultTemplatesSourceProvider(this, settings, NuPkgs, Folders);
                allCreatedProviders.Add(new WeakReference<DefaultTemplatesSourceProvider>(defaultTemplatesSourceProvider));
                return defaultTemplatesSourceProvider;
            }

            static IEnumerable<string> Folders { get; set; }
            static IEnumerable<string> NuPkgs { get; set; }

            public static void SetNuPkgsAndFolders(IEnumerable<string> nupkgs = null, IEnumerable<string> folders = null)
            {
                NuPkgs = nupkgs;
                Folders = folders;
            }

            static List<WeakReference<DefaultTemplatesSourceProvider>> allCreatedProviders = new List<WeakReference<DefaultTemplatesSourceProvider>>();

            public static void TriggerChanged()
            {
                foreach (var provider in allCreatedProviders)
                {
                    if(provider.TryGetTarget(out var actualProvider))
                    {
                        actualProvider.TriggerSourcesChangedEvent();
                    }
                }
            }
        }

        EnvironmentSettingsHelper helper = new EnvironmentSettingsHelper();

        private static string GetNupkgsFolder()
        {
            var thisDir = Path.GetDirectoryName(typeof(SettingsLoaderTests).Assembly.Location);
            return Path.Combine(thisDir, "..", "..", "..", "..", "..", "test", "Microsoft.TemplateEngine.TestTemplates", "nupkg_templates");
        }

        [Fact]
        public async Task RebuildCacheIfNotCurrentScansAll()
        {
            var engineEnvironmentSettings = helper.CreateEnvironment();

            var nupkgFolder = GetNupkgsFolder();
            var nupkgsWildcard = new[] { Path.Combine(nupkgFolder, "*.nupkg") };

            FakeFactory.SetNuPkgsAndFolders(nupkgsWildcard);
            engineEnvironmentSettings.SettingsLoader.Components.Register(typeof(FakeFactory));
            await engineEnvironmentSettings.SettingsLoader.RebuildCacheFromSettingsIfNotCurrent(false);

            var allNupkgs = Directory.GetFiles(nupkgFolder);
            // All mount points should have been scanned
            AssertMountPointsWereOpened(allNupkgs, engineEnvironmentSettings);
        }

        [Fact]
        public async Task RebuildCacheSkipsNonAccessibleMounts()
        {
            var engineEnvironmentSettings = helper.CreateEnvironment();
            string nupkgFolder = GetNupkgsFolder();
            var validAndInvalidNuPkg = new[] { Directory.GetFiles(nupkgFolder, "*.nupkg")[0], Path.Combine(nupkgFolder, $"{new Guid()}.nupkg") };

            FakeFactory.SetNuPkgsAndFolders(validAndInvalidNuPkg);
            engineEnvironmentSettings.SettingsLoader.Components.Register(typeof(FakeFactory));
            await engineEnvironmentSettings.SettingsLoader.RebuildCacheFromSettingsIfNotCurrent(false);

            var allNupkgs = validAndInvalidNuPkg.Take(1);
            // All mount points should have been scanned
            AssertMountPointsWereOpened(allNupkgs, engineEnvironmentSettings);
        }

        [Fact]
        public async Task RebuildCacheIfForceRebuildScansAll()
        {
            var engineEnvironmentSettings = helper.CreateEnvironment();

            var nupkgFolder = GetNupkgsFolder();
            var nupkgsWildcard = new[] { Path.Combine(nupkgFolder, "*.nupkg") };

            FakeFactory.SetNuPkgsAndFolders(nupkgsWildcard);
            engineEnvironmentSettings.SettingsLoader.Components.Register(typeof(FakeFactory));
            await engineEnvironmentSettings.SettingsLoader.RebuildCacheFromSettingsIfNotCurrent(false);

            var allNupkgs = Directory.GetFiles(nupkgFolder);
            // All mount points should have been scanned
            AssertMountPointsWereOpened(allNupkgs, engineEnvironmentSettings);

            var monitoredFileSystem = (MonitoredFileSystem)engineEnvironmentSettings.Host.FileSystem;

            monitoredFileSystem.Reset();
            await engineEnvironmentSettings.SettingsLoader.RebuildCacheFromSettingsIfNotCurrent(false);
            // Make sure that we don't rescan with force=false
            AssertMountPointsWereOpened(Array.Empty<string>(), engineEnvironmentSettings);

            monitoredFileSystem.Reset();
            await engineEnvironmentSettings.SettingsLoader.RebuildCacheFromSettingsIfNotCurrent(true);
            // Make sure that we rescan with force=false
            AssertMountPointsWereOpened(allNupkgs, engineEnvironmentSettings);
        }

        [Fact]
        public async Task RebuildCacheFromSettingsOnlyScansOutOfDateFileSystemMountPoints()
        {
            var engineEnvironmentSettings = helper.CreateEnvironment();

            var tmpFolder = helper.CreateTemporaryFolder();
            foreach (var item in Directory.GetFiles(GetNupkgsFolder()))
            {
                File.Copy(item, Path.Combine(tmpFolder, Path.GetFileName(item)));
            }

            var nupkgsWildcard = new[] { Path.Combine(tmpFolder, "*.nupkg") };

            FakeFactory.SetNuPkgsAndFolders(nupkgsWildcard);
            engineEnvironmentSettings.SettingsLoader.Components.Register(typeof(FakeFactory));
            await engineEnvironmentSettings.SettingsLoader.RebuildCacheFromSettingsIfNotCurrent(false);

            var allNupkgs = Directory.GetFiles(tmpFolder);
            // All mount points should have been scanned
            AssertMountPointsWereOpened(allNupkgs, engineEnvironmentSettings);

            var monitoredFileSystem = (MonitoredFileSystem)engineEnvironmentSettings.Host.FileSystem;

            monitoredFileSystem.Reset();
            await engineEnvironmentSettings.SettingsLoader.RebuildCacheFromSettingsIfNotCurrent(false);
            // Make sure that we don't rescan with force=false
            AssertMountPointsWereOpened(Array.Empty<string>(), engineEnvironmentSettings);

            // Update LastWriteTime on one of files:
            var modifiedFile = Directory.GetFiles(tmpFolder)[0];
            File.SetLastWriteTimeUtc(modifiedFile, DateTime.UtcNow.AddSeconds(5));

            FakeFactory.TriggerChanged();

            monitoredFileSystem.Reset();
            await engineEnvironmentSettings.SettingsLoader.RebuildCacheFromSettingsIfNotCurrent(false);
            // Make sure that we rescan with force=false
            AssertMountPointsWereOpened(new[] { modifiedFile }, engineEnvironmentSettings);
        }

        [Fact]
        public async Task EnsureCacheRoundtripPreservesTemplateWithLocaleTimestamp()
        {
            var engineEnvironmentSettings = helper.CreateEnvironment("en-GB");

            var nupkgFolder = GetNupkgsFolder();
            var nupkgsWildcard = new[] { Path.Combine(nupkgFolder, "*.nupkg") };

            FakeFactory.SetNuPkgsAndFolders(nupkgsWildcard);
            engineEnvironmentSettings.SettingsLoader.Components.Register(typeof(FakeFactory));
            await engineEnvironmentSettings.SettingsLoader.RebuildCacheFromSettingsIfNotCurrent(false);

            var allNupkgs = Directory.GetFiles(nupkgFolder);
            // All mount points should have been scanned
            AssertMountPointsWereOpened(allNupkgs, engineEnvironmentSettings);

            ((SettingsLoader)engineEnvironmentSettings.SettingsLoader).Save();

            var monitoredFileSystem = (MonitoredFileSystem)engineEnvironmentSettings.Host.FileSystem;

            FakeFactory.TriggerChanged();

            monitoredFileSystem.Reset();
            await engineEnvironmentSettings.SettingsLoader.RebuildCacheFromSettingsIfNotCurrent(false);
            // Make sure that we don't rescan with force=false
            AssertMountPointsWereOpened(Array.Empty<string>(), engineEnvironmentSettings);
        }


        [Fact]
        public async Task RemoveMountpointRemovesTemplates()
        {
            var engineEnvironmentSettings = helper.CreateEnvironment();

            var nupkgFolder = GetNupkgsFolder();
            var allNupkgs = Directory.GetFiles(nupkgFolder).Select(Path.GetFullPath).ToList();

            FakeFactory.SetNuPkgsAndFolders(allNupkgs);
            engineEnvironmentSettings.SettingsLoader.Components.Register(typeof(FakeFactory));
            var templatesAll = await engineEnvironmentSettings.SettingsLoader.GetTemplatesAsync(default);

            // All mount points should have been scanned
            AssertMountPointsWereOpened(allNupkgs, engineEnvironmentSettings);

            // All mountpoints/sources should have at least 1 template
            Assert.Equal(allNupkgs.OrderBy(m => m), templatesAll.Select(t => t.MountPointUri).Distinct().OrderBy(m => m));

            var monitoredFileSystem = (MonitoredFileSystem)engineEnvironmentSettings.Host.FileSystem;

            monitoredFileSystem.Reset();

            //Remove all but 1
            allNupkgs.RemoveRange(1, allNupkgs.Count - 1);
            FakeFactory.TriggerChanged();

            var templatesOnly1 = await engineEnvironmentSettings.SettingsLoader.GetTemplatesAsync(default);
            // Make sure that we don't rescan anything, since we only removed sources
            AssertMountPointsWereOpened(Array.Empty<string>(), engineEnvironmentSettings);

            // Make sure that templates return only have MountPointUri of our remaining nupkg
            Assert.Equal(allNupkgs, templatesOnly1.Select(t => t.MountPointUri).Distinct().OrderBy(m => m));
        }

        private void AssertMountPointsWereScanned(IEnumerable<string> mountPoints, IEngineEnvironmentSettings environmentSettings)
        {
            string[] expectedScannedDirectories = mountPoints
                .Select(x => x)
                .OrderBy(x => x)
                .ToArray();
            string[] actualScannedDirectories = ((MonitoredFileSystem)environmentSettings.Host.FileSystem).DirectoriesScanned
                .Select(dir => Path.Combine(dir.DirectoryName, dir.Pattern))
                .OrderBy(x => x)
                .ToArray();

            Assert.Equal(expectedScannedDirectories, actualScannedDirectories);
        }

        private void AssertMountPointsWereOpened(IEnumerable<string> mountPoints, IEngineEnvironmentSettings environmentSettings)
        {
            string[] expectedScannedDirectories = mountPoints
                .Select(f => Path.GetFullPath(f))
                .OrderBy(x => x)
                .ToArray();
            string[] actualScannedDirectories = ((MonitoredFileSystem)environmentSettings.Host.FileSystem).FilesOpened
                .Select(f => Path.GetFullPath(f))
                .OrderBy(x => x)
                .ToArray();

            Assert.Equal(expectedScannedDirectories, actualScannedDirectories);
        }

        private void AssertMountPointsWereNotScanned(IEnumerable<string> mountPoints, IEngineEnvironmentSettings environmentSettings)
        {
            IEnumerable<string> expectedScannedDirectories = mountPoints;
            IEnumerable<string> actualScannedDirectories = ((MonitoredFileSystem)environmentSettings.Host.FileSystem).DirectoriesScanned.Select(dir => Path.Combine(dir.DirectoryName, dir.Pattern));
            Assert.Empty(actualScannedDirectories.Intersect(expectedScannedDirectories));
        }

        public void Dispose() => helper.Dispose();
    }
}
