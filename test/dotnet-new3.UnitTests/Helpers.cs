// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public static string HomeEnvironmentVariableName { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "USERPROFILE" : "HOME";

        public static string CreateTemporaryFolder([CallerMemberName] string name = "")
        {
            string workingDir = Path.Combine(Path.GetTempPath(), "DotnetNew3_Tests", Guid.NewGuid().ToString(), name);
            Directory.CreateDirectory(workingDir);
            return workingDir;
        }
        internal static string GetTestTemplateLocation(string templateName)
        {
            string codebase = typeof(Program).GetTypeInfo().Assembly.Location;
            Uri cb = new Uri(codebase);
            string asmPath = cb.LocalPath;
            string dir = Path.GetDirectoryName(asmPath);
            string testTemplate = Path.Combine(dir, "..", "..", "..", "..", "..", "test", "Microsoft.TemplateEngine.TestTemplates", "test_templates", templateName);
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
        internal static string PackNuGetPackage(string projectPath, string outputFolder)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                throw new ArgumentException("projectPath cannot be null", nameof(projectPath));
            }
            string absolutePath = Path.GetFullPath(projectPath);
            if (!File.Exists(projectPath))
            {
                throw new ArgumentException($"{projectPath} doesn't exist", nameof(projectPath));
            }

            var _info = new ProcessStartInfo("dotnet", $"pack {absolutePath} -o {outputFolder}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            Process p = Process.Start(_info);
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                throw new Exception($"Failed to pack the project {projectPath}");
            }

            string createdPackagePath = Directory.GetFiles(outputFolder).Aggregate(
                (latest, current) => (latest == null) ? current : File.GetCreationTimeUtc(current) > File.GetCreationTimeUtc(latest) ? current : latest);
            return createdPackagePath;
        }

        internal static string PackProjectTemplatesNuGetPackage(string templatePackName, string outputFolder)
        {
            string dir = Path.GetDirectoryName(typeof(Helpers).GetTypeInfo().Assembly.Location);
            string projectToPack = Path.Combine(dir, "..", "..", "..", "..", "..", "template_feed", templatePackName, $"{templatePackName}.csproj");
            return PackNuGetPackage(projectToPack, outputFolder);
        }

        internal static string PackTestTemplatesNuGetPackage(string outputFolder)
        {
            string dir = Path.GetDirectoryName(typeof(Helpers).GetTypeInfo().Assembly.Location);
            string projectToPack = Path.Combine(dir, "..", "..", "..", "..", "..", "test", "Microsoft.TemplateEngine.TestTemplates", "Microsoft.TemplateEngine.TestTemplates.csproj");
            return PackNuGetPackage(projectToPack, outputFolder);
        }
    }
}
