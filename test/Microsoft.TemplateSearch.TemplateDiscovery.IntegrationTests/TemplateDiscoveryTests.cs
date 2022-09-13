﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateEngine.TestHelper.Commands;
using Microsoft.TemplateEngine.Tests;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.TemplateSearch.TemplateDiscovery.IntegrationTests
{
    public class TemplateDiscoveryTests : TestBase
    {
        private readonly ITestOutputHelper _log;

        public TemplateDiscoveryTests(ITestOutputHelper log)
        {
            _log = log;
        }

        [Fact]
        public async Task CanRunDiscoveryTool()
        {
            string testDir = TestUtils.CreateTemporaryFolder();
            using var packageManager = new PackageManager();
            string packageLocation = PackTestTemplatesNuGetPackage(packageManager);
            packageLocation = await packageManager.GetNuGetPackage("Microsoft.Azure.WebJobs.ProjectTemplates").ConfigureAwait(false);

            new DotnetCommand(
                _log,
                "Microsoft.TemplateSearch.TemplateDiscovery.dll",
                "--basePath",
                testDir,
                "--packagesPath",
                Path.GetDirectoryName(packageLocation) ?? throw new Exception("Couldn't get package location directory"),
                "-v")
                .Execute()
                .Should()
                .ExitWith(0);

            string[] cacheFilePaths = new[]
            {
                Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfo.json"),
                Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfoVer2.json")
            };
            var settingsPath = TestUtils.CreateTemporaryFolder();

            foreach (var cacheFilePath in cacheFilePaths)
            {
                Assert.True(File.Exists(cacheFilePath));
                new DotnetCommand(_log, "new")
                      .WithCustomHive(settingsPath)
                      .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
                      .WithEnvironmentVariable("DOTNET_CLI_CONTEXT_VERBOSE", "true")
                      .Execute()
                      .Should()
                      .ExitWith(0)
                      .And.NotHaveStdErr();

                new DotnetCommand(_log, "new", "func", "--search")
                    .WithCustomHive(settingsPath)
                    .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
                    .WithEnvironmentVariable("DOTNET_CLI_CONTEXT_VERBOSE", "true")
                    .Execute()
                    .Should()
                    .ExitWith(0)
                    .And.NotHaveStdErr()
                    .And.NotHaveStdOutContaining("Exception")
                    .And.HaveStdOutContaining("Microsoft.Azure.WebJobs.ProjectTemplates");

                new DotnetCommand(_log, "new")
                      .WithCustomHive(settingsPath)
                      .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
                      .WithEnvironmentVariable("DOTNET_CLI_CONTEXT_VERBOSE", "true")
                      .Execute()
                      .Should()
                      .ExitWith(0)
                      .And.NotHaveStdErr();

                new DotnetCommand(_log, "new", "func", "--search")
                    .WithCustomHive(settingsPath)
                    .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
                    .WithEnvironmentVariable("DOTNET_CLI_CONTEXT_VERBOSE", "true")
                    .Execute()
                    .Should()
                    .ExitWith(0)
                    .And.NotHaveStdErr()
                    .And.NotHaveStdOutContaining("Exception")
                    .And.HaveStdOutContaining("Microsoft.Azure.WebJobs.ProjectTemplates");
            }
        }

        [Fact]
        public void CanReadAuthor()
        {
            string testDir = TestUtils.CreateTemporaryFolder();
            using var packageManager = new PackageManager();
            string packageLocation = PackTestTemplatesNuGetPackage(packageManager);

            new DotnetCommand(
                _log,
                "Microsoft.TemplateSearch.TemplateDiscovery.dll",
                "--basePath",
                testDir,
                "--packagesPath",
                Path.GetDirectoryName(packageLocation) ?? throw new Exception("Couldn't get package location directory"),
                "-v")
                .Execute()
                .Should()
                .ExitWith(0);

            var jObjectV1 = JObject.Parse(File.ReadAllText(Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfo.json")))!;
            Assert.Equal("TestAuthor", jObjectV1!["PackToTemplateMap"]!.Children<JProperty>().Single(p => p.Name.StartsWith("Microsoft.TemplateEngine.TestTemplates")).Value["Owners"]!.Values().Single());
            var jObjectV2 = JObject.Parse(File.ReadAllText(Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfoVer2.json")))!;
            Assert.Equal("TestAuthor", jObjectV2!["TemplatePackages"]![0]!["Owners"]!.Value<string>());
        }

        [Fact]
        public void CanReadDescription()
        {
            string testDir = TestUtils.CreateTemporaryFolder();
            using var packageManager = new PackageManager();
            string packageLocation = PackTestTemplatesNuGetPackage(packageManager);

            new DotnetCommand(
                _log,
                "Microsoft.TemplateSearch.TemplateDiscovery.dll",
                "--basePath",
                testDir,
                "--packagesPath",
                Path.GetDirectoryName(packageLocation) ?? throw new Exception("Couldn't get package location directory"),
                "-v")
                .Execute()
                .Should()
                .ExitWith(0);

            var jObjectV2 = JObject.Parse(File.ReadAllText(Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfoVer2.json")))!;
            Assert.Equal("description", jObjectV2!["TemplatePackages"]![0]!["Description"]!.Value<string>());
        }

        [Fact]
        public void CanReadIconUrl()
        {
            string testDir = TestUtils.CreateTemporaryFolder();
            using var packageManager = new PackageManager();
            string packageLocation = PackTestTemplatesNuGetPackage(packageManager);

            new DotnetCommand(
                _log,
                "Microsoft.TemplateSearch.TemplateDiscovery.dll",
                "--basePath",
                testDir,
                "--packagesPath",
                Path.GetDirectoryName(packageLocation) ?? throw new Exception("Couldn't get package location directory"),
                "-v")
                .Execute()
                .Should()
                .ExitWith(0);

            var jObjectV2 = JObject.Parse(File.ReadAllText(Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfoVer2.json")))!;
            Assert.Equal("https://icon", jObjectV2!["TemplatePackages"]![0]!["IconUrl"]!.Value<string>());
        }

        [Fact]
        public async Task CanDetectNewPackagesInDiffMode()
        {
            string testDir = TestUtils.CreateTemporaryFolder();
            using var packageManager = new PackageManager();
            string packageLocation = PackTestTemplatesNuGetPackage(packageManager);

            File.Move(packageLocation, Path.Combine(Path.GetDirectoryName(packageLocation)!, "Test.Templates##1.0.0.nupkg"));

            new DotnetCommand(
                _log,
                "Microsoft.TemplateSearch.TemplateDiscovery.dll",
                "--basePath",
                testDir,
                "--packagesPath",
                Path.GetDirectoryName(packageLocation) ?? throw new Exception("Couldn't get package location directory"),
                "-v",
                "--diff",
                "false")
                .Execute()
                .Should()
                .ExitWith(0)
                .And.HaveStdOutContaining(
@"Template packages:
   new: 1
      Test.Templates::1.0.0
   updated: 0
   removed: 0
   not changed: 0")
                .And.HaveStdOutContaining(
@"Non template packages:
   new: 0
   updated: 0
   removed: 0
   not changed: 0");

            string cacheV1Path = Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfo.json");
            string cacheV2Path = Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfoVer2.json");
            string nonTemplatePackagesList = Path.Combine(testDir, "SearchCache", "nonTemplatePacks.json");

            Assert.True(File.Exists(cacheV1Path));
            Assert.True(File.Exists(cacheV2Path));
            Assert.True(File.Exists(nonTemplatePackagesList));

            packageLocation = await packageManager.GetNuGetPackage("Microsoft.Azure.WebJobs.ProjectTemplates").ConfigureAwait(false);

            File.Move(packageLocation, Path.Combine(Path.GetDirectoryName(packageLocation)!, "Microsoft.Azure.WebJobs.ProjectTemplates##1.0.0.nupkg"));

            new DotnetCommand(
                _log,
                "Microsoft.TemplateSearch.TemplateDiscovery.dll",
                "--basePath",
                testDir,
                "--packagesPath",
                Path.GetDirectoryName(packageLocation) ?? throw new Exception("Couldn't get package location directory"),
                "-v",
                "--diff",
                "true",
                "--diff-override-cache",
                cacheV2Path)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.HaveStdOutContaining(
@"Template packages:
   new: 1
      Microsoft.Azure.WebJobs.ProjectTemplates::1.0.0
   updated: 0
   removed: 0
   not changed: 1")
                .And.HaveStdOutContaining(
@"Non template packages:
   new: 0
   updated: 0
   removed: 0
   not changed: 0");

            Assert.True(File.Exists(cacheV1Path));
            Assert.True(File.Exists(cacheV2Path));
            Assert.True(File.Exists(nonTemplatePackagesList));

            var jObjectV1 = JObject.Parse(File.ReadAllText(cacheV1Path));
            Assert.Equal(2, jObjectV1["PackToTemplateMap"]?.Children<JProperty>().Count());
            var jObjectV2 = JObject.Parse(File.ReadAllText(cacheV2Path));
            Assert.Equal(2, jObjectV2["TemplatePackages"]?.Count());
        }

        [Fact]
        public void CanDetectUpdatedPackagesInDiffMode()
        {
            string testDir = TestUtils.CreateTemporaryFolder();
            using var packageManager = new PackageManager();
            string packageLocation = PackTestTemplatesNuGetPackage(packageManager);

            string testFileName = Path.Combine(Path.GetDirectoryName(packageLocation)!, "Test.Templates##1.0.0.nupkg");
            File.Move(packageLocation, testFileName);

            new DotnetCommand(
                _log,
                "Microsoft.TemplateSearch.TemplateDiscovery.dll",
                "--basePath",
                testDir,
                "--packagesPath",
                Path.GetDirectoryName(packageLocation) ?? throw new Exception("Couldn't get package location directory"),
                "-v",
                "--diff",
                "false")
                .Execute()
                .Should()
                .ExitWith(0)
                .And.HaveStdOutContaining(
@"Template packages:
   new: 1
      Test.Templates::1.0.0
   updated: 0
   removed: 0
   not changed: 0")
                .And.HaveStdOutContaining(
@"Non template packages:
   new: 0
   updated: 0
   removed: 0
   not changed: 0");

            string cacheV1Path = Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfo.json");
            string cacheV2Path = Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfoVer2.json");
            string nonTemplatePackagesList = Path.Combine(testDir, "SearchCache", "nonTemplatePacks.json");

            Assert.True(File.Exists(cacheV1Path));
            Assert.True(File.Exists(cacheV2Path));
            Assert.True(File.Exists(nonTemplatePackagesList));

            File.Move(testFileName, Path.Combine(Path.GetDirectoryName(testFileName)!, "Test.Templates##1.0.1.nupkg"));

            new DotnetCommand(
                _log,
                "Microsoft.TemplateSearch.TemplateDiscovery.dll",
                "--basePath",
                testDir,
                "--packagesPath",
                Path.GetDirectoryName(packageLocation) ?? throw new Exception("Couldn't get package location directory"),
                "-v",
                "--diff",
                "true",
                "--diff-override-cache",
                cacheV2Path)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.HaveStdOutContaining(
@"Template packages:
   new: 0
   updated: 1
      Test.Templates, 1.0.0 --> 1.0.1
   removed: 0
   not changed: 0")
                .And.HaveStdOutContaining(
@"Non template packages:
   new: 0
   updated: 0
   removed: 0
   not changed: 0");

            Assert.True(File.Exists(cacheV1Path));
            Assert.True(File.Exists(cacheV2Path));
            Assert.True(File.Exists(nonTemplatePackagesList));

            var jObjectV1 = JObject.Parse(File.ReadAllText(cacheV1Path));
            Assert.Equal(1, jObjectV1["PackToTemplateMap"]?.Children<JProperty>().Count());
            var jObjectV2 = JObject.Parse(File.ReadAllText(cacheV2Path));
            Assert.Equal(1, jObjectV2["TemplatePackages"]?.Count());
            Assert.Equal("1.0.1", jObjectV2["TemplatePackages"]?[0]?["Version"]?.Value<string>());
        }

        [Fact]
        public void CanDetectRemovedPackagesInDiffMode()
        {
            string testDir = TestUtils.CreateTemporaryFolder();
            using var packageManager = new PackageManager();
            string packageLocation = PackTestTemplatesNuGetPackage(packageManager);

            string testFileName = Path.Combine(Path.GetDirectoryName(packageLocation)!, "Test.Templates##1.0.0.nupkg");
            File.Move(packageLocation, testFileName);

            new DotnetCommand(
                _log,
                "Microsoft.TemplateSearch.TemplateDiscovery.dll",
                "--basePath",
                testDir,
                "--packagesPath",
                Path.GetDirectoryName(packageLocation) ?? throw new Exception("Couldn't get package location directory"),
                "-v",
                "--diff",
                "false")
                .Execute()
                .Should()
                .ExitWith(0)
                .And.HaveStdOutContaining(
@"Template packages:
   new: 1
      Test.Templates::1.0.0
   updated: 0
   removed: 0
   not changed: 0")
                .And.HaveStdOutContaining(
@"Non template packages:
   new: 0
   updated: 0
   removed: 0
   not changed: 0");

            string cacheV1Path = Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfo.json");
            string cacheV2Path = Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfoVer2.json");
            string nonTemplatePackagesList = Path.Combine(testDir, "SearchCache", "nonTemplatePacks.json");

            Assert.True(File.Exists(cacheV1Path));
            Assert.True(File.Exists(cacheV2Path));
            Assert.True(File.Exists(nonTemplatePackagesList));

            File.Delete(testFileName);

            new DotnetCommand(
                _log,
                "Microsoft.TemplateSearch.TemplateDiscovery.dll",
                "--basePath",
                testDir,
                "--packagesPath",
                Path.GetDirectoryName(packageLocation) ?? throw new Exception("Couldn't get package location directory"),
                "-v",
                "--diff",
                "true",
                "--diff-override-cache",
                cacheV2Path)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.HaveStdOutContaining(
@"Template packages:
   new: 0
   updated: 0
   removed: 1
      Test.Templates::1.0.0
   not changed: 0")
                .And.HaveStdOutContaining(
@"Non template packages:
   new: 0
   updated: 0
   removed: 0
   not changed: 0")
                .And.HaveStdOutContaining(
@"[Error]: the following 1 packages were removed
   Test.Templates::1.0.0
Checking template packages via API: 
Package Test.Templates was unlisted."
                );

            Assert.True(File.Exists(cacheV1Path));
            Assert.True(File.Exists(cacheV2Path));
            Assert.True(File.Exists(nonTemplatePackagesList));

            var jObjectV1 = JObject.Parse(File.ReadAllText(cacheV1Path));
            Assert.Equal(0, jObjectV1["PackToTemplateMap"]?.Children<JProperty>().Count());
            var jObjectV2 = JObject.Parse(File.ReadAllText(cacheV2Path));
            Assert.Equal(0, jObjectV2["TemplatePackages"]?.Count());
        }

#pragma warning disable xUnit1004 // Test methods should not be skipped
        [Fact(Skip = "Template options filtering is not implemented.")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
        public void CanReadCliData()
        {
            string testDir = TestUtils.CreateTemporaryFolder();
            using var packageManager = new PackageManager();
            string packageLocation = PackTestTemplatesNuGetPackage(packageManager);

            new DotnetCommand(
                _log,
                "Microsoft.TemplateSearch.TemplateDiscovery.dll",
                "--basePath",
                testDir,
                "--packagesPath",
                Path.GetDirectoryName(packageLocation) ?? throw new Exception("Couldn't get package location directory"),
                "-v")
                .Execute()
                .Should()
                .ExitWith(0);

            string[] cacheFilePaths = new[]
            {
                Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfo.json"),
                Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfoVer2.json")
            };
            var settingsPath = TestUtils.CreateTemporaryFolder();
            CheckTemplateOptionsSearch(cacheFilePaths, settingsPath);
        }

#pragma warning disable xUnit1004 // Test methods should not be skipped
        [Fact(Skip = "Template options filtering is not implemented.")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
        public void CanReadCliDataFromDiff()
        {
            string testDir = TestUtils.CreateTemporaryFolder();
            using var packageManager = new PackageManager();
            string packageLocation = PackTestTemplatesNuGetPackage(packageManager);

            new DotnetCommand(
                _log,
                "Microsoft.TemplateSearch.TemplateDiscovery.dll",
                "--basePath",
                testDir,
                "--packagesPath",
                Path.GetDirectoryName(packageLocation) ?? throw new Exception("Couldn't get package location directory"),
                "-v",
                "--diff",
                "false")
                .Execute()
                .Should()
                .ExitWith(0);

            string[] cacheFilePaths = new[]
            {
                Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfo.json"),
                Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfoVer2.json")
            };
            var settingsPath = TestUtils.CreateTemporaryFolder();
            CheckTemplateOptionsSearch(cacheFilePaths, settingsPath);

            string testDir2 = TestUtils.CreateTemporaryFolder();
            new DotnetCommand(
                _log,
                "Microsoft.TemplateSearch.TemplateDiscovery.dll",
                "--basePath",
                testDir2,
                "--packagesPath",
                Path.GetDirectoryName(packageLocation) ?? throw new Exception("Couldn't get package location directory"),
                "-v",
                "--diff",
                "true",
                "--diff-override-cache",
                Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfoVer2.json"))
                .Execute()
                .Should()
                .ExitWith(0)
                .And.HaveStdOutContaining("not changed: 1");

            string[] updatedCacheFilePaths = new[]
            {
                Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfo.json"),
                Path.Combine(testDir, "SearchCache", "NuGetTemplateSearchInfoVer2.json")
            };
            CheckTemplateOptionsSearch(updatedCacheFilePaths, settingsPath);
        }

        private void CheckTemplateOptionsSearch(IEnumerable<string> cacheFilePaths, string settingsPath)
        {
            foreach (var cacheFilePath in cacheFilePaths)
            {
                Assert.True(File.Exists(cacheFilePath));
                new DotnetCommand(_log, "new")
                      .WithCustomHive(settingsPath)
                      .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
                      .WithEnvironmentVariable("DOTNET_CLI_CONTEXT_VERBOSE", "true")
                      .Execute()
                      .Should()
                      .ExitWith(0)
                      .And.NotHaveStdErr();

                new DotnetCommand(_log, "new", "CliHostFile", "--search")
                    .WithCustomHive(settingsPath)
                    .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
                    .WithEnvironmentVariable("DOTNET_CLI_CONTEXT_VERBOSE", "true")
                    .Execute()
                    .Should()
                    .ExitWith(0)
                    .And.NotHaveStdErr()
                    .And.NotHaveStdOutContaining("Exception")
                    .And.HaveStdOutContaining("TestAssets.TemplateWithCliHostFile")
                    .And.HaveStdOutContaining("Microsoft.TemplateEngine.TestTemplates");

                new DotnetCommand(_log, "new", "--search", "--param")
                     .WithCustomHive(settingsPath)
                     .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
                     .WithEnvironmentVariable("DOTNET_CLI_CONTEXT_VERBOSE", "true")
                     .Execute()
                     .Should()
                     .ExitWith(0)
                     .And.NotHaveStdErr()
                     .And.NotHaveStdOutContaining("Exception")
                     .And.HaveStdOutContaining("TestAssets.TemplateWithCliHostFile")
                     .And.HaveStdOutContaining("Microsoft.TemplateEngine.TestTemplates");

                new DotnetCommand(_log, "new", "--search", "-p")
                    .WithCustomHive(settingsPath)
                    .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
                    .WithEnvironmentVariable("DOTNET_CLI_CONTEXT_VERBOSE", "true")
                    .Execute()
                    .Should()
                    .ExitWith(0)
                    .And.NotHaveStdErr()
                    .And.NotHaveStdOutContaining("Exception")
                    .And.HaveStdOutContaining("TestAssets.TemplateWithCliHostFile")
                    .And.HaveStdOutContaining("Microsoft.TemplateEngine.TestTemplates");

                new DotnetCommand(_log, "new", "--search", "--test-param")
                    .WithCustomHive(settingsPath)
                    .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
                    .WithEnvironmentVariable("DOTNET_CLI_CONTEXT_VERBOSE", "true")
                    .Execute()
                    .Should().Fail();
            }
        }
    }
}
