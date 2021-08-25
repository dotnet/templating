// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using FluentAssertions;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using Microsoft.TemplateEngine.TestHelper;

namespace Microsoft.TemplateSearch.TemplateDiscovery.Test
{
    internal static class CacheFileTests60
    {
        public static void RunTests(string cacheFileV2Path)
        {
            cacheFileV2Path = Path.GetFullPath(cacheFileV2Path);
            Console.WriteLine($"Running tests for: {cacheFileV2Path}.");
            string workingDirectory = TestUtils.CreateTemporaryFolder();
            UseSdkVersion(workingDirectory, requestedSdkVersion: "6.0.100-rc.2.21420.30", resolvedVersionPattern: "6.0.", rollForward: "latestFeature");
            CanCheckUpdates(workingDirectory, cacheFileV2Path);
            CanUpdate(workingDirectory, cacheFileV2Path);
            CanSearch(workingDirectory, cacheFileV2Path);
            Console.WriteLine($"Tests fo V2 succeeded.");
        }

        private static void UseSdkVersion(string workingDirectory, string requestedSdkVersion, string resolvedVersionPattern, string rollForward = "latestMinor", bool allowPrerelease = false)
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
            var settingsPath = TestUtils.CreateTemporaryFolder();
            new DotnetCommand(TestOutputLogger.Instance, "new", "func", "--debug:custom-hive", settingsPath)
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
            var settingsPath = TestUtils.CreateTemporaryFolder();
            new DotnetCommand(TestOutputLogger.Instance, "new", "--install", "Microsoft.Azure.WebJobs.ItemTemplates::2.1.1785", "--debug:custom-hive", settingsPath)
              .WithWorkingDirectory(workingDirectory)
              .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
              .Execute()
              .Should()
              .ExitWith(0)
              .And.NotHaveStdErr();

            new DotnetCommand(TestOutputLogger.Instance, "new", "--update-check", "--debug:custom-hive", settingsPath)
                .WithWorkingDirectory(workingDirectory)
                .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.NotHaveStdOutContaining("Exception")
                .And.HaveStdOutContaining("An update for template packages is available:")
                .And.HaveStdOutMatching("Microsoft.Azure.WebJobs.ItemTemplates.*2.1.1785");
        }

        private static void CanUpdate(string workingDirectory, string cacheFilePath)
        {
            var settingsPath = TestUtils.CreateTemporaryFolder();
            new DotnetCommand(TestOutputLogger.Instance, "new", "--install", "Microsoft.Azure.WebJobs.ItemTemplates::2.1.1785", "--debug:custom-hive", settingsPath)
              .WithWorkingDirectory(workingDirectory)
              .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
              .Execute()
              .Should()
              .ExitWith(0)
              .And.NotHaveStdErr();

            new DotnetCommand(TestOutputLogger.Instance, "new", "--update-apply", "--debug:custom-hive", settingsPath)
                .WithWorkingDirectory(workingDirectory)
                .WithEnvironmentVariable("DOTNET_NEW_SEARCH_FILE_OVERRIDE", cacheFilePath)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.NotHaveStdOutContaining("Exception")
                .And.HaveStdOutMatching("Success: Microsoft.Azure.WebJobs.ItemTemplates::.\\..\\..... installed the following templates");
        }

        private static void CanSearch(string workingDirectory, string cacheFilePath)
        {
            var settingsPath = TestUtils.CreateTemporaryFolder();
            new DotnetCommand(TestOutputLogger.Instance, "new", "func", "--search", "--debug:custom-hive", settingsPath)
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
