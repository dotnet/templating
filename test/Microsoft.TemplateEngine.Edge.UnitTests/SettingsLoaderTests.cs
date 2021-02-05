using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using AutoFixture;
using AutoFixture.Kernel;
using FakeItEasy;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Edge.Mount.FileSystem;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Mocks;
using Microsoft.TemplateEngine.TestHelper;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.TemplateEngine.Edge.UnitTests
{
    public class SettingsLoaderTests : TestBase
    {
        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly MockFileSystem _fileSystem;
        private readonly IFixture _fixture;

        public SettingsLoaderTests()
        {
            _fixture = new Fixture();
            _fixture.Customizations.Add(new TemplateInfoBuilder());

            _fileSystem = new MockFileSystem
            {
                CurrentDirectory = Environment.CurrentDirectory
            };
            _environmentSettings = A.Fake<IEngineEnvironmentSettings>();

            A.CallTo(() => _environmentSettings.Host.FileSystem)
                .Returns(_fileSystem);
            A.CallTo(() => _environmentSettings.Paths.BaseDir)
                .Returns(BaseDir);
        }

        public string BaseDir
        {
            get
            {
                bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                string profileDir = Environment.GetEnvironmentVariable(isWindows
                    ? "USERPROFILE"
                    : "HOME");

                return Path.Combine(profileDir, ".tetestrunner");
            }
        }

        [Fact(DisplayName = nameof(RebuildCacheIfNotCurrentScansAll), Skip = "Rewrite to make sense again")]
        public void RebuildCacheIfNotCurrentScansAll()
        {
            _fixture.Customizations.Add(new stringBuilder());
            List<string> mountPoints = _fixture.CreateMany<string>().ToList();
            List<TemplateInfo> templates = TemplatesFromMountPoints(mountPoints);

            SetupUserSettings(isCurrentVersion: false, mountPoints: mountPoints);
            SetupTemplates(templates);

            MockMountPointManager mockMountPointManager = new MockMountPointManager(_environmentSettings);
            SettingsLoader subject = new SettingsLoader(_environmentSettings, mockMountPointManager);

            subject.RebuildCacheFromSettingsIfNotCurrent(false);

            // All mount points should have been scanned
            AssertMountPointsWereScanned(mountPoints);
        }

        [Fact(DisplayName = nameof(RebuildCacheSkipsNonAccessibleMounts), Skip = "Rewrite to make sense again")]
        public void RebuildCacheSkipsNonAccessibleMounts()
        {
            _fixture.Customizations.Add(new stringBuilder());
            List<string> availableMountPoints = _fixture.CreateMany<string>().ToList();
            List<string> unavailableMountPoints = _fixture.CreateMany<string>().ToList();
            List<string> allMountPoints = availableMountPoints.Concat(unavailableMountPoints).ToList();

            List<TemplateInfo> templates = TemplatesFromMountPoints(allMountPoints);

            SetupUserSettings(isCurrentVersion: false, mountPoints: allMountPoints);
            SetupTemplates(templates);

            MockMountPointManager mockMountPointManager = new MockMountPointManager(_environmentSettings);
            mockMountPointManager.UnavailableMountPoints.AddRange(unavailableMountPoints);
            SettingsLoader subject = new SettingsLoader(_environmentSettings, mockMountPointManager);

            subject.RebuildCacheFromSettingsIfNotCurrent(false);

            // All mount points should have been scanned
            AssertMountPointsWereScanned(availableMountPoints);
            AssertMountPointsWereNotScanned(unavailableMountPoints);
        }


        [Fact(DisplayName = nameof(RebuildCacheIfForceRebuildScansAll), Skip = "Rewrite to make sense again")]
        public void RebuildCacheIfForceRebuildScansAll()
        {
            _fixture.Customizations.Add(new stringBuilder());
            List<string> mountPoints = _fixture.CreateMany<string>().ToList();
            List<TemplateInfo> templates = TemplatesFromMountPoints(mountPoints);

            SetupUserSettings(isCurrentVersion: true, mountPoints: mountPoints);
            SetupTemplates(templates);

            MockMountPointManager mockMountPointManager = new MockMountPointManager(_environmentSettings);
            SettingsLoader subject = new SettingsLoader(_environmentSettings, mockMountPointManager);

            subject.RebuildCacheFromSettingsIfNotCurrent(true);

            // All mount points should have been scanned
            AssertMountPointsWereScanned(mountPoints);
        }

        [Fact(DisplayName = nameof(RebuildCacheFromSettingsOnlyScansOutOfDateFileSystemMountPoints), Skip = "Rewrite to make sense again")]
        public void RebuildCacheFromSettingsOnlyScansOutOfDateFileSystemMountPoints()
        {
            _fixture.Customizations.Add(new stringBuilder(FileSystemMountPointFactory.FactoryId));
            List<string> mountPoints = _fixture.Build<string>()
                .CreateMany()
                .ToList();
            List<TemplateInfo> templates = TemplatesFromMountPoints(mountPoints);
            
            DateTime oldTimestamp = new DateTime(2018,1,1);
            DateTime recentTimestamp = new DateTime(2018, 9, 28);
            DateTime moreRecentTimestamp = new DateTime(2018, 9, 29);
            foreach (TemplateInfo templateInfo in templates)
            {
                string mountPoint =
                    mountPoints.Single(mp => mp == templateInfo.MountPointUri);

                // The first template has a recent timestamp in the cache, but a more
                // recent one on disk
                templateInfo.ConfigTimestampUtc = templateInfo == templates.First()
                    ? recentTimestamp
                    : oldTimestamp;
                DateTime fileTimestamp = templateInfo == templates.First()
                    ? moreRecentTimestamp
                    : oldTimestamp;

                string pathToTemplateFile = Path.Combine(mountPoint, templateInfo.ConfigPlace.TrimStart('/'));
                _fileSystem.Add(pathToTemplateFile, "{}", lastWriteTime: fileTimestamp);
            }

            SetupUserSettings(isCurrentVersion: true, mountPoints: mountPoints);
            SetupTemplates(templates);

            MockMountPointManager mockMountPointManager = new MockMountPointManager(_environmentSettings);
            SettingsLoader subject = new SettingsLoader(_environmentSettings, mockMountPointManager);

            subject.RebuildCacheFromSettingsIfNotCurrent(false);

            // Only the first mount point should have been scanned
            AssertMountPointsWereScanned(mountPoints.Take(1));
        }



        [Fact(DisplayName = nameof(EnsureCacheRoundtripPerservesTemplateWithLocaleTimestamp))]
        public void EnsureCacheRoundtripPerservesTemplateWithLocaleTimestamp()
        {
            var environmentSettings = A.Fake<IEngineEnvironmentSettings>();

            A.CallTo(() => environmentSettings.Host.FileSystem)
                .Returns(_fileSystem);
            A.CallTo(() => environmentSettings.Paths.BaseDir)
                .Returns(BaseDir);
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-GB");

            _fixture.Customizations.Add(new stringBuilder(FileSystemMountPointFactory.FactoryId));
            List<string> mountPoints = _fixture.Build<string>()
                .CreateMany()
                .ToList();
            List<TemplateInfo> templates = TemplatesFromMountPoints(mountPoints);

            DateTime timestamp = new DateTime(2018, 1, 1);
            foreach (TemplateInfo templateInfo in templates)
            {
                string mountPoint =
                    mountPoints.Single(mp => mp == templateInfo.MountPointUri);

                templateInfo.ConfigTimestampUtc = timestamp;

                string pathToTemplateFile = Path.Combine(mountPoint, templateInfo.ConfigPlace.TrimStart('/'));
                _fileSystem.Add(pathToTemplateFile, "{}", lastWriteTime: timestamp);
            }

            SetupUserSettings(isCurrentVersion: true, mountPoints: mountPoints);
            SetupTemplates(templates);
            SettingsLoader subject = new SettingsLoader(environmentSettings);
            A.CallTo(() => environmentSettings.SettingsLoader)
                .Returns(subject);
            subject.Reload();
            subject.Save();
            subject.RebuildCacheFromSettingsIfNotCurrent(false);

            // Only the first mount point should have been scanned
            AssertMountPointsWereScanned(Enumerable.Empty<string>());
        }

        private void SetupUserSettings(bool isCurrentVersion = true, IEnumerable<string> mountPoints = null)
        {
            SettingsStore userSettings = new SettingsStore();

            if (isCurrentVersion)
            {
                userSettings.SetVersionToCurrent();
            }

            JObject serialized = JObject.FromObject(userSettings);
            _fileSystem.Add(Path.Combine(BaseDir, "settings.json"), serialized.ToString());
        }

        private void SetupTemplates(List<TemplateInfo> templates)
        {
            TemplateCache cache = new TemplateCache(_environmentSettings, templates);

            JObject serialized = JObject.FromObject(cache);
            _fileSystem.Add(Path.Combine(BaseDir, "templatecache.json"), serialized.ToString());
        }

        private List<TemplateInfo> TemplatesFromMountPoints(IEnumerable<string> mountPoints)
        {
            return mountPoints.Select(mp => _fixture
                    .Build<TemplateInfo>()
                    .With(x => x.MountPointUri, mp)
                    .Create())
                .ToList();
        }

        private void AssertMountPointsWereScanned(IEnumerable<string> mountPoints)
        {
            string[] expectedScannedDirectories = mountPoints
                .Select(x => x)
                .OrderBy(x => x)
                .ToArray();
            string[] actualScannedDirectories = _fileSystem.DirectoriesScanned
                .Select(dir => Path.Combine(dir.DirectoryName, dir.Pattern))
                .OrderBy(x => x)
                .ToArray();

            Assert.Equal(expectedScannedDirectories, actualScannedDirectories);
        }
        private void AssertMountPointsWereNotScanned(IEnumerable<string> mountPoints)
        {
            IEnumerable <string> expectedScannedDirectories = mountPoints;
            IEnumerable<string> actualScannedDirectories = _fileSystem.DirectoriesScanned.Select(dir => Path.Combine(dir.DirectoryName, dir.Pattern));
            Assert.Empty(actualScannedDirectories.Intersect(expectedScannedDirectories));
        }

        public class stringBuilder : ISpecimenBuilder
        {
            private readonly Guid? _mountPointFactoryId;

            public stringBuilder(Guid? mountPointFactoryId = null)
            {
                _mountPointFactoryId = mountPointFactoryId;
            }

            public object Create(object request, ISpecimenContext context)
            {
                if (!(request is ParameterInfo pi))
                {
                    return new NoSpecimen();
                }

                //if (pi.Member.DeclaringType == typeof(string) &&
                //    pi.ParameterType == typeof(string) &&
                //    pi.Name == "place")
                //{
                //    bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                //    if (isWindows)
                //    {
                //        return Path.Combine(@"C:\", context.Create<string>(), context.Create<string>());
                //    }
                //    else
                //    {
                //        return Path.Combine(@"/", context.Create<string>(), context.Create<string>());
                //    }    
                //}

                //if (pi.Member.DeclaringType == typeof(string) &&
                //    pi.ParameterType == typeof(Guid) &&
                //    pi.Name == "mountPointFactoryId" &&
                //    _mountPointFactoryId.HasValue)
                //{
                //    return _mountPointFactoryId;
                //}

                return new NoSpecimen();
            }
        }

        public class TemplateInfoBuilder : ISpecimenBuilder
        {
            public object Create(object request, ISpecimenContext context)
            {
                if (!(request is PropertyInfo pi))
                {
                    return new NoSpecimen();
                }

                if (pi.PropertyType == typeof(IReadOnlyDictionary<string, IBaselineInfo>))
                {
                    return new Dictionary<string, IBaselineInfo>();
                }

                if (pi.PropertyType == typeof(IReadOnlyDictionary<string, ICacheParameter>))
                {
                    return new Dictionary<string, ICacheParameter>();
                }

                if (pi.PropertyType == typeof(IReadOnlyDictionary<string, ICacheTag>))
                {
                    return new Dictionary<string, ICacheTag>();
                }

                return new NoSpecimen();
            }
        }
    }
}
