// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet_new3.IntegrationTests
{
    public class CommonTemplatesTests : IClassFixture<SharedHomeDirectory>
    {
        private readonly SharedHomeDirectory _fixture;
        private readonly ITestOutputHelper _log;

        public CommonTemplatesTests(SharedHomeDirectory fixture, ITestOutputHelper log)
        {
            _fixture = fixture;
            _log = log;
        }

        [Theory]
        [InlineData("Console Application", "console")]
        [InlineData("Console Application", "console", "C#")]
        [InlineData("Console Application", "console", "F#")]
        [InlineData("Console Application", "console", "VB")]
        [InlineData("Console Application", "console", "C#", "netcoreapp3.1")]
        [InlineData("Console Application", "console", "F#", "netcoreapp3.1")]
        [InlineData("Console Application", "console", "VB", "netcoreapp3.1")]
        [InlineData("Console Application", "console", "C#", "netcoreapp2.1")]
        [InlineData("Console Application", "console", "F#", "netcoreapp2.1")]
        [InlineData("Console Application", "console", "VB", "netcoreapp2.1")]

        [InlineData("Class library", "classlib")]
        [InlineData("Class library", "classlib", "C#")]
        [InlineData("Class library", "classlib", "F#")]
        [InlineData("Class library", "classlib", "VB")]
        [InlineData("Class library", "classlib", "C#", "netcoreapp3.1")]
        [InlineData("Class library", "classlib", "F#", "netcoreapp3.1")]
        [InlineData("Class library", "classlib", "VB", "netcoreapp3.1")]
        [InlineData("Class library", "classlib", "C#", "netcoreapp2.1")]
        [InlineData("Class library", "classlib", "F#", "netcoreapp2.1")]
        [InlineData("Class library", "classlib", "VB", "netcoreapp2.1")]
        [InlineData("Class library", "classlib", "C#", "netstandard2.1")]
        [InlineData("Class library", "classlib", "VB", "netstandard2.1")]
        [InlineData("Class library", "classlib", "F#", "netstandard2.1")]
        [InlineData("Class library", "classlib", "C#", "netstandard2.0")]
        [InlineData("Class library", "classlib", "VB", "netstandard2.0")]
        [InlineData("Class library", "classlib", "F#", "netstandard2.0")]

        public void AllCommonProjectsCreateRestoreAndBuild(string expectedTemplateName, string templateShortName, string language = null, string framework = null, string langVersion = null)
        {
            string workingDir = Helpers.CreateTemporaryFolder();
            string workingDirName = Path.GetFileName(workingDir);
            string extension = "csproj";

            switch (language)
            {
                case "F#":
                    extension = "fsproj";
                    break;
                case "VB":
                    extension = "vbproj";
                    break;
            }

            string finalProjectName = Regex.Escape(Path.Combine(workingDir, $"{workingDirName}.{extension}"));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                //on OSX path in restore starts from /private for some reason
                finalProjectName = "/private" + finalProjectName;
            }
            Console.WriteLine($"Expected project location: {finalProjectName}");

            List<string> args = new List<string>() { templateShortName };
            if (!string.IsNullOrWhiteSpace(language))
            {
                args.Add("--language");
                args.Add(language);
            }
            if (!string.IsNullOrWhiteSpace(framework))
            {
                args.Add("--framework");
                args.Add(framework);
            }
            if (!string.IsNullOrWhiteSpace(langVersion))
            {
                args.Add("--langVersion");
                args.Add(langVersion);
            }

            new DotnetNewCommand(_log, args.ToArray())
                .WithCustomHive(_fixture.HomeDirectory)
                .WithWorkingDirectory(workingDir)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutMatching(
$@"The template ""{expectedTemplateName}"" was created successfully\.

Processing post-creation actions\.\.\.
Running 'dotnet restore' on ({finalProjectName})\.\.\.
  Determining projects to restore\.\.\.
(.*$\n)*  Restored ({finalProjectName}) \(in \d{{1,5}}(\.\d{{1,5}}){{0,1}} \w*\)\.

Restore succeeded\.",
                RegexOptions.Multiline);

            new DotnetCommand(_log, "restore")
                .WithWorkingDirectory(workingDir)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr();

            new DotnetCommand(_log, "build")
                .WithWorkingDirectory(workingDir)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr();

            Directory.Delete(workingDir, true);
        }

        [Theory]
        [InlineData("Console Application", "console")]
        [InlineData("Console Application", "console", "C#")]
        [InlineData("Console Application", "console", "F#")]
        [InlineData("Console Application", "console", "VB")]
        [InlineData("Console Application", "console", "C#", "netcoreapp3.1")]
        [InlineData("Console Application", "console", "F#", "netcoreapp3.1")]
        [InlineData("Console Application", "console", "VB", "netcoreapp3.1")]
        [InlineData("Console Application", "console", "C#", "netcoreapp2.1")]
        [InlineData("Console Application", "console", "F#", "netcoreapp2.1")]
        [InlineData("Console Application", "console", "VB", "netcoreapp2.1")]

        [InlineData("Class library", "classlib")]
        [InlineData("Class library", "classlib", "C#")]
        [InlineData("Class library", "classlib", "F#")]
        [InlineData("Class library", "classlib", "VB")]
        [InlineData("Class library", "classlib", "C#", "netcoreapp3.1")]
        [InlineData("Class library", "classlib", "F#", "netcoreapp3.1")]
        [InlineData("Class library", "classlib", "VB", "netcoreapp3.1")]
        [InlineData("Class library", "classlib", "C#", "netcoreapp2.1")]
        [InlineData("Class library", "classlib", "F#", "netcoreapp2.1")]
        [InlineData("Class library", "classlib", "VB", "netcoreapp2.1")]
        [InlineData("Class library", "classlib", "C#", "netstandard2.1")]
        [InlineData("Class library", "classlib", "VB", "netstandard2.1")]
        [InlineData("Class library", "classlib", "F#", "netstandard2.1")]
        [InlineData("Class library", "classlib", "C#", "netstandard2.0")]
        [InlineData("Class library", "classlib", "VB", "netstandard2.0")]
        [InlineData("Class library", "classlib", "F#", "netstandard2.0")]
        public void AllCommonProjectsCreate_NoRestore(string expectedTemplateName, string templateShortName, string language = null, string framework = null)
        {
            string workingDir = Helpers.CreateTemporaryFolder();

            List<string> args = new List<string>() { templateShortName, "--no-restore" };
            if (!string.IsNullOrWhiteSpace(language))
            {
                args.Add("--language");
                args.Add(language);
            }
            if (!string.IsNullOrWhiteSpace(framework))
            {
                args.Add("--framework");
                args.Add(framework);
            }

            new DotnetNewCommand(_log, args.ToArray())
                .WithCustomHive(_fixture.HomeDirectory)
                .WithWorkingDirectory(workingDir)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining($@"The template ""{expectedTemplateName}"" was created successfully.");

            Directory.Delete(workingDir, true);
        }

        [Theory]
        [InlineData("dotnet gitignore file", "gitignore")]
        [InlineData("global.json file", "globaljson")]
        [InlineData("NuGet Config", "nugetconfig")]
        [InlineData("Solution File", "sln")]
        [InlineData("Dotnet local tool manifest file", "tool-manifest")]
        [InlineData("Web Config", "webconfig")]
        public void AllCommonItemsCreate(string expectedTemplateName, string templateShortName)
        {
            string workingDir = Helpers.CreateTemporaryFolder();

            new DotnetNewCommand(_log, templateShortName)
                .WithCustomHive(_fixture.HomeDirectory)
                .WithWorkingDirectory(workingDir)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining($@"The template ""{expectedTemplateName}"" was created successfully.");

            Directory.Delete(workingDir, true);
        }

        [Fact]
        public void NuGetConfigPermissions()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                //runs only on Unix
                return;
            }

            string templateShortName = "nugetconfig";
            string expectedTemplateName = "NuGet Config";
            string workingDir = Helpers.CreateTemporaryFolder();

            new DotnetNewCommand(_log, templateShortName)
                .WithCustomHive(_fixture.HomeDirectory)
                .WithWorkingDirectory(workingDir)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining($@"The template ""{expectedTemplateName}"" was created successfully.");

            var process = Process.Start(new ProcessStartInfo()
                    {
                        FileName = "/bin/sh",
                        Arguments = "-c \"ls -la\"",
                        WorkingDirectory = workingDir
                    });

            new Command(process)
                .WorkingDirectory(workingDir)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .ExitWith(0)
                .And.HaveStdOutMatching("^-rw-------.*nuget.config$", RegexOptions.Multiline);

            Directory.Delete(workingDir, true);
        }

        [Theory]
        [InlineData(
@"{
  ""sdk"": {
    ""version"": ""5.0.200""
  }
}",
            "globaljson",
            "--sdk-version",
            "5.0.200")]
        public void GlobalJsonTests(string expectedContent, params string[] parameters)
        {
            string workingDir = Helpers.CreateTemporaryFolder();

            new DotnetNewCommand(_log, parameters)
                .WithCustomHive(_fixture.HomeDirectory)
                .WithWorkingDirectory(workingDir)
                .Execute()
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOut($@"The template ""global.json file"" was created successfully.");

            string globalJsonConent = File.ReadAllText(Path.Combine(workingDir, "global.json"));
            Assert.Equal(expectedContent.Replace("\r\n", "\n"), globalJsonConent.Replace("\r\n", "\n"));
            Directory.Delete(workingDir, true);
        }
    }
}
