// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using FluentAssertions;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using Microsoft.TemplateEngine.TestHelper;

namespace Microsoft.TemplateSearch.TemplateDiscovery.Test
{
    internal static class CacheFileTests
    {
        public static void RunTests(string cacheFilePath)
        {
            string workingDirectory = TestUtils.CreateTemporaryFolder();
            CanUseSdkVersion(workingDirectory, "3.1.400", "3.");
            CanSearchWhileInstantiating(workingDirectory, cacheFilePath);
            CanCheckUpdates(workingDirectory, cacheFilePath);
            CanUpdate(workingDirectory, cacheFilePath);

            workingDirectory = TestUtils.CreateTemporaryFolder();
            CanUseSdkVersion(workingDirectory, "5.0.100", "5.0.1", "latestPatch");
            CanSearchWhileInstantiating(workingDirectory, cacheFilePath);
            CanCheckUpdates(workingDirectory, cacheFilePath);
            CanUpdate(workingDirectory, cacheFilePath);

            workingDirectory = TestUtils.CreateTemporaryFolder();
            CanUseSdkVersion(workingDirectory, "5.0.300", "5.0.", "latestFeature");
            CanCheckUpdates(workingDirectory, cacheFilePath);
            CanUpdate(workingDirectory, cacheFilePath);
            CanSearch(workingDirectory, cacheFilePath);
        }

        private static void CanUseSdkVersion(string workingDirectory, string requestedSdkVersion, string resolvedVersionPattern, string rollForward = "latestMinor", bool allowPrerelease = false)
        {
            CreateGlobalJson(workingDirectory, requestedSdkVersion, rollForward, allowPrerelease);

            new DotnetCommand(TestOutputLogger.Instance, "--version")
                .WithWorkingDirectory(workingDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining(resolvedVersionPattern);
        }

        private static void CanSearchWhileInstantiating(string workingDirectory, string cacheFilePath)
        {
            new DotnetCommand(TestOutputLogger.Instance, "new", "func")
                .WithWorkingDirectory(workingDirectory)
                .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.HaveStdOutContaining("Couldn't find an installed template that matches the input, searching online for one that does...")
                .And.HaveStdOutContaining("Template name \"Azure Functions\" (func) from author \"Microsoft\" in pack Microsoft.Azure.WebJobs.ProjectTemplates");
        }

        private static void CanCheckUpdates(string workingDirectory, string cacheFilePath)
        {
            new DotnetCommand(TestOutputLogger.Instance, "new", "--update-check")
                .WithWorkingDirectory(workingDirectory)
                .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.NotHaveStdOutContaining("Exception")
                .And.HaveStdOutMatching("(Updates are available for the following:|No updates were found.)");
        }

        private static void CanUpdate(string workingDirectory, string cacheFilePath)
        {
            new DotnetCommand(TestOutputLogger.Instance, "new", "--update-apply")
                .WithWorkingDirectory(workingDirectory)
                .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.NotHaveStdOutContaining("Exception")
                .And.HaveStdOutMatching("(...Update succeeded|No updates were found.)");
        }

        private static void CanSearch(string workingDirectory, string cacheFilePath)
        {
            new DotnetCommand(TestOutputLogger.Instance, "new", "func", "--search")
                .WithWorkingDirectory(workingDirectory)
                .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.NotHaveStdOutContaining("Exception")
                .And.HaveStdOutContaining("Microsoft.Azure.Functions.Worker.ProjectTemplates");
        }

        private static void CreateGlobalJson(string directory, string sdkVersion, string rollForward = "latestMinor", bool allowPrerelease = false)
        {
            string prereleaseSection = allowPrerelease ? @", ""allowPrerelease"": ""true""" : string.Empty;
            string jsonContent = $@"{{ ""sdk"": {{ ""version"": ""{sdkVersion}"", ""rollForward"": ""{rollForward}"" {prereleaseSection}}} }}";
            File.WriteAllText(Path.Combine(directory, "global.json"), jsonContent);
        }
    }
}
