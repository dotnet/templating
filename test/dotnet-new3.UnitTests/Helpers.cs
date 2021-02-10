// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FluentAssertions;
using Microsoft.NET.TestFramework.Assertions;
using Xunit.Abstractions;

namespace dotnet_new3.UnitTests
{
    internal static class Helpers
    {
        public static string CreateTemporaryFolder([CallerMemberName] string name = "")
        {
            string workingDir = Path.Combine(Path.GetTempPath(), "DotnetNew3_Tests", Guid.NewGuid().ToString(), name);
            Directory.CreateDirectory(workingDir);
            return workingDir;
        }

        public static string HomeEnvironmentVariableName { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "USERPROFILE" : "HOME";

        internal static string GetTestTemplateLocation(string templateName)
        {
            string codebase = typeof(Program).GetTypeInfo().Assembly.Location;
            Uri cb = new Uri(codebase);
            string asmPath = cb.LocalPath;
            string dir = Path.GetDirectoryName(asmPath);
            string testTemplate = Path.Combine(dir, "..", "..", "..", "..", "..", "test", "Microsoft.TemplateEngine.TestTemplates", "test_templates", templateName);
            return Path.GetFullPath(testTemplate);
        }

        internal static string InstallTestTemplate(string templateName, ITestOutputHelper log, string workingDirectory, string homeDirectory)
        {
            string testTemplate = GetTestTemplateLocation(templateName);
            new DotnetNewCommand(log, "-i", testTemplate)
                  .WithWorkingDirectory(workingDirectory)
                  .WithEnvironmentVariable(HomeEnvironmentVariableName, homeDirectory)
                  .Execute()
                  .Should()
                  .ExitWith(0)
                  .And
                  .NotHaveStdErr();
            return Path.GetFullPath(testTemplate);
        }

        internal static void InstallNuGetTemplate(string packageName, ITestOutputHelper log, string workingDirectory, string homeDirectory)
        {
            new DotnetNewCommand(log, "-i", packageName)
                  .WithWorkingDirectory(workingDirectory)
                  .WithEnvironmentVariable(HomeEnvironmentVariableName, homeDirectory)
                  .Execute()
                  .Should()
                  .ExitWith(0)
                  .And
                  .NotHaveStdErr();
        }
    }
}
