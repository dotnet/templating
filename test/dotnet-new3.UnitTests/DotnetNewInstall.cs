// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.NET.TestFramework.Assertions;
using System;
using System.IO;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace dotnet_new3.UnitTests
{
    public class DotnetNewInstallTests
    {
        private readonly ITestOutputHelper _log;

        public DotnetNewInstallTests(ITestOutputHelper log)
        {
            _log = log;
        }

        [Fact]
        public void CanInstallRemoteNuGetPackage()
        {
            var home = Helpers.CreateTemporaryFolder("Home");
            new DotnetNewCommand(_log, "-i", "Microsoft.DotNet.Web.ProjectTemplates.5.0", "--quiet")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.NotHaveStdOutContaining("Determining projects to restore...")
                .And.HaveStdOutContaining("The following template packages will be installed:")
                .And.HaveStdOutMatching($"Success: Microsoft\\.DotNet\\.Web\\.ProjectTemplates\\.5\\.0::([\\d\\.a-z-])+ installed the following templates:")
                .And.HaveStdOutContaining("web")
                .And.HaveStdOutContaining("blazorwasm");
        }

        [Fact]
        public void CanInstallRemoteNuGetPackageWithVersion()
        {
            var home = Helpers.CreateTemporaryFolder("Home");
            new DotnetNewCommand(_log, "-i", "Microsoft.DotNet.Web.ProjectTemplates.5.0::5.0.0", "--quiet")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.NotHaveStdOutContaining("Determining projects to restore...")
                .And.HaveStdOutContaining("The following template packages will be installed:")
                .And.HaveStdOutContaining("Microsoft.DotNet.Web.ProjectTemplates.5.0, version: 5.0.0")
                .And.HaveStdOutContaining($"Success: Microsoft.DotNet.Web.ProjectTemplates.5.0::5.0.0 installed the following templates:")
                .And.HaveStdOutContaining("web")
                .And.HaveStdOutContaining("blazorwasm");
        }

        [Fact]
        public void CanInstallRemoteNuGetPackageWithNuGetSource()
        {
            var home = Helpers.CreateTemporaryFolder("Home");
            new DotnetNewCommand(_log, "-i", "Take.Blip.Client.Templates", "--quiet", "--nuget-source", "https://api.nuget.org/v3/index.json")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining("The following template packages will be installed:")
                .And.HaveStdOutMatching($"Success: Take\\.Blip\\.Client\\.Templates::([\\d\\.a-z-])+ installed the following templates:")
                .And.HaveStdOutContaining("blip-console");

            new DotnetNewCommand(_log, "-i", "Take.Blip.Client.Templates", "--quiet", "--add-source", "https://api.nuget.org/v3/index.json")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining("The following template packages will be installed:")
                .And.HaveStdOutMatching($"Success: Take\\.Blip\\.Client\\.Templates::([\\d\\.a-z-])+ installed the following templates:")
                .And.HaveStdOutContaining("blip-console");
        }

        [Fact]
        public void CanInstallLocalNuGetPackage()
        {
            var home = Helpers.CreateTemporaryFolder("Home");
            var outputFolder = Helpers.CreateTemporaryFolder();

            var packageLocation = Helpers.PackTestTemplatesNuGetPackage(outputFolder);

            new DotnetNewCommand(_log, "-i", packageLocation)
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should().ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("The following template packages will be installed:")
                .And.HaveStdOutMatching($"Success: Microsoft\\.TemplateEngine\\.TestTemplates::([\\d\\.a-z-])+ installed the following templates:")
                .And.HaveStdOutContaining("TestAssets.TemplateWithTags")
                .And.HaveStdOutContaining("TestAssets.ConfigurationKitchenSink");
        }

        [Fact]
        public void CanInstallLocalFolder()
        {
            var home = Helpers.CreateTemporaryFolder("Home");
            string basicFSharp = Helpers.GetTestTemplateLocation("TemplateResolution/DifferentLanguagesGroup/BasicFSharp");
            new DotnetNewCommand(_log, "-i", basicFSharp)
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining("The following template packages will be installed:")
                .And.HaveStdOutContaining($"Success: {basicFSharp} installed the following templates:")
                .And.HaveStdOutContaining("basic");
        }

        [Fact]
        public void PrintOnlyNewlyInstalledTemplates()
        {
            var home = Helpers.CreateTemporaryFolder("Home");

            new DotnetNewCommand(_log, "-i", "Microsoft.DotNet.Common.ProjectTemplates.5.0", "--quiet")
               .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
               .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
               .Execute()
               .Should()
               .ExitWith(0)
               .And
               .NotHaveStdErr()
               .And.HaveStdOutContaining("console")
               .And.HaveStdOutContaining("Console Application");

            new DotnetNewCommand(_log, "-i", "Microsoft.DotNet.Web.ProjectTemplates.5.0", "--quiet")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining("web")
                .And.HaveStdOutContaining("blazorwasm")
                .And.NotHaveStdOutContaining("console");
        }

        [Fact]
        public void CannotInstallUnknownRemotePackage()
        {
            var home = Helpers.CreateTemporaryFolder("Home");

            new DotnetNewCommand(_log, "-i", "BlaBlaBla", "--quiet")
               .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
               .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
               .Execute()
               .Should().Fail()
               .And.HaveStdErrContaining("BlaBlaBla could not be installed, the package does not exist.");
        }

        [Fact]
        public void CannotInstallRemotePackageWithIncorrectVersion()
        {
            var home = Helpers.CreateTemporaryFolder("Home");

            new DotnetNewCommand(_log, "-i", "Microsoft.DotNet.Web.ProjectTemplates.5.0::16.0.0", "--quiet")
               .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
               .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
               .Execute()
               .Should().Fail()
               .And.HaveStdErrContaining("Microsoft.DotNet.Web.ProjectTemplates.5.0::16.0.0 could not be installed, the package does not exist.");
        }

        [Fact(Skip = "https://github.com/dotnet/templating/issues/2857")]
        public void CanInstallSeveralSources()
        {
            var home = Helpers.CreateTemporaryFolder("Home");
            string basicFSharp = Helpers.GetTestTemplateLocation("TemplateResolution/DifferentLanguagesGroup/BasicFSharp");
            string basicVB = Helpers.GetTestTemplateLocation("TemplateResolution/DifferentLanguagesGroup/BasicVB");

            new DotnetNewCommand(_log, "-i", "Microsoft.DotNet.Web.ProjectTemplates.5.0", "-i", "Microsoft.DotNet.Common.ProjectTemplates.5.0", "-i", basicFSharp, "-i", basicVB, "--quiet")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.NotHaveStdOutContaining("Determining projects to restore...")
                .And.HaveStdOutContaining("web")
                .And.HaveStdOutContaining("blazorwasm")
                .And.HaveStdOutContaining("console")
                .And.HaveStdOutContaining("classlib")
                .And.HaveStdOutContaining("basic");
        }

        [Fact]
        public void CannotInstallSameSourceTwice_NuGet()
        {
            var home = Helpers.CreateTemporaryFolder("Home");

            new DotnetNewCommand(_log, "-i", "Microsoft.DotNet.Common.ProjectTemplates.5.0::5.0.0")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining("console")
                .And.HaveStdOutContaining("classlib");

            new DotnetNewCommand(_log, "-i", "Microsoft.DotNet.Common.ProjectTemplates.5.0::5.0.0")
                 .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                 .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                 .Execute()
                 .Should().Fail()
                 .And.HaveStdErrContaining("Microsoft.DotNet.Common.ProjectTemplates.5.0::5.0.0 is already installed.");
        }

        [Fact]
        public void CannotInstallSameSourceTwice_Folder()
        {
            var home = Helpers.CreateTemporaryFolder("Home");
            string basicFSharp = Helpers.GetTestTemplateLocation("TemplateResolution/DifferentLanguagesGroup/BasicFSharp");
            new DotnetNewCommand(_log, "-i", basicFSharp)
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining("basic");

            new DotnetNewCommand(_log, "-i", basicFSharp)
                 .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                 .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                 .Execute()
                 .Should().Fail()
                 .And.HaveStdErrContaining($"{basicFSharp} is already installed.");
        }

        [Fact(Skip = "https://github.com/dotnet/templating/issues/2857")]
        public void CanUpdateSameSource_NuGet()
        {
            var home = Helpers.CreateTemporaryFolder("Home");

            new DotnetNewCommand(_log, "-i", "Microsoft.DotNet.Common.ProjectTemplates.5.0::5.0.0", "--quiet")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining("console")
                .And.HaveStdOutContaining("classlib");

            new DotnetNewCommand(_log, "-u")
                 .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                 .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                 .Execute()
                 .Should().ExitWith(0)
                 .And.NotHaveStdErr()
                 .And.HaveStdOutContaining("Microsoft.DotNet.Common.ProjectTemplates.5.0")
                 .And.HaveStdOutContaining("Version: 5.0.0")
                 .And.NotHaveStdOutContaining("Version: 5.0.1");

            Assert.True(File.Exists(Path.Combine(home, ".templateengine", "packages", "Microsoft.DotNet.Common.ProjectTemplates.5.0.5.0.0.nupkg")));

            new DotnetNewCommand(_log, "-i", "Microsoft.DotNet.Common.ProjectTemplates.5.0::5.0.1")
                 .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                 .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                 .Execute()
                 .Should().ExitWith(0)
                 .And.NotHaveStdErr()
                 .And.HaveStdOutContaining("The following template packages will be installed:")
                 .And.HaveStdOutContaining("Microsoft.DotNet.Common.ProjectTemplates.5.0, version: 5.0.1")
                 .And.HaveStdOutContaining("Microsoft.DotNet.Common.ProjectTemplates.5.0 is already installed, version: 5.0.0, it will be replaced with version 5.0.1.")
                 .And.HaveStdOutContaining("Microsoft.DotNet.Common.ProjectTemplates.5.0::5.0.0 was successfully uninstalled.")
                 .And.HaveStdOutContaining($"Success: Microsoft.DotNet.Common.ProjectTemplates.5.0::5.0.1 installed the following templates:")
                 .And.HaveStdOutContaining("console")
                 .And.HaveStdOutContaining("classlib");

            new DotnetNewCommand(_log, "-u")
                 .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                 .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                 .Execute()
                 .Should().ExitWith(0)
                 .And.NotHaveStdErr()
                 .And.HaveStdOutContaining("Microsoft.DotNet.Common.ProjectTemplates.5.0")
                 .And.HaveStdOutContaining("Version: 5.0.1")
                 .And.NotHaveStdOutContaining("Version: 5.0.0");

            Assert.False(File.Exists(Path.Combine(home, ".templateengine", "packages", "Microsoft.DotNet.Common.ProjectTemplates.5.0.5.0.0.nupkg")));
            Assert.True(File.Exists(Path.Combine(home, ".templateengine", "packages", "Microsoft.DotNet.Common.ProjectTemplates.5.0.5.0.1.nupkg")));
        }

        [Fact(Skip = "https://github.com/dotnet/templating/issues/2857")]
        public void InstallingSamePackageFromRemoteUpdatesLocal()
        {
            var home = Helpers.CreateTemporaryFolder("Home");
            var outputFolder = Helpers.CreateTemporaryFolder();

            var packageLocation = Helpers.PackProjectTemplatesNuGetPackage("Microsoft.DotNet.Common.ProjectTemplates.5.0", outputFolder);

            new DotnetNewCommand(_log, "-i", packageLocation)
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should().ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutMatching($"Success: Microsoft\\.DotNet\\.Common\\.ProjectTemplates\\.5\\.0::([\\d\\.a-z-])+ installed the following templates:")
                .And.HaveStdOutContaining("console")
                .And.HaveStdOutContaining("classlib");

            new DotnetNewCommand(_log, "-u")
                 .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                 .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                 .Execute()
                 .Should().ExitWith(0)
                 .And.NotHaveStdErr()
                 .And.HaveStdOutContaining("Microsoft.DotNet.Common.ProjectTemplates.5.0")
                 .And.HaveStdOutContaining("Author: Microsoft")
                 .And.HaveStdOutContaining("Version:")
                 .And.NotHaveStdOutContaining("Version: 5.0.0");

            new DotnetNewCommand(_log, "-i", "Microsoft.DotNet.Common.ProjectTemplates.5.0::5.0.0")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should().ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("The following template packages will be installed:")
                .And.HaveStdOutContaining("Microsoft.DotNet.Common.ProjectTemplates.5.0, version: 5.0.0")
                .And.HaveStdOutMatching("Microsoft\\.DotNet\\.Common\\.ProjectTemplates\\.5\\.0 is already installed, version: ([\\d\\.a-z-])+, it will be replaced with version 5\\.0\\.0\\.")
                .And.HaveStdOutMatching("Microsoft\\.DotNet\\.Common\\.ProjectTemplates\\.5\\.0::([\\d\\.a-z-])+ was successfully uninstalled\\.")
                .And.HaveStdOutContaining($"Success: Microsoft.DotNet.Common.ProjectTemplates.5.0::5.0.0 installed the following templates:")
                .And.HaveStdOutContaining("console")
                .And.HaveStdOutContaining("classlib");

            new DotnetNewCommand(_log, "-u")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should().ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("Microsoft.DotNet.Common.ProjectTemplates.5.0")
                .And.HaveStdOutContaining("Author: Microsoft")
                .And.HaveStdOutContaining("Version: 5.0.0");
        }

        [Fact]
        public void CanExpandWhenInstall()
        {
            var home = Helpers.CreateTemporaryFolder("Home");
            var outputFolder = Helpers.CreateTemporaryFolder();

            string codebase = typeof(Program).GetTypeInfo().Assembly.Location;
            Uri cb = new Uri(codebase);
            string asmPath = cb.LocalPath;
            string dir = Path.GetDirectoryName(asmPath);
            string testTemplateLocation = Path.Combine(dir, "..", "..", "..", "..", "..", "test", "Microsoft.TemplateEngine.TestTemplates", "test_templates");
            string testTemplateLocationAbsolute = Path.GetFullPath(testTemplateLocation);
            string pattern = testTemplateLocation + Path.DirectorySeparatorChar + "*";


            new DotnetNewCommand(_log, "-i", pattern)
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should().ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOutContaining("The following template packages will be installed:")
                .And.HaveStdOutContaining(Path.Combine(testTemplateLocationAbsolute, "ConfigurationKitchenSink"))
                .And.HaveStdOutContaining(Path.Combine(testTemplateLocationAbsolute, "TemplateResolution"))
                .And.HaveStdOutContaining(Path.Combine(testTemplateLocationAbsolute, "TemplateWithSourceName"))
                .And.HaveStdOutContaining($"Success: {Path.Combine(testTemplateLocationAbsolute, "ConfigurationKitchenSink")} installed the following templates:")
                .And.HaveStdOutContaining($"Success: {Path.Combine(testTemplateLocationAbsolute, "TemplateResolution")} installed the following templates:")
                .And.HaveStdOutContaining($"Success: {Path.Combine(testTemplateLocationAbsolute, "TemplateWithSourceName")} installed the following templates:")
                .And.HaveStdOutContaining("basic")
                .And.HaveStdOutContaining("TestAssets.ConfigurationKitchenSink");
        }

        [Fact]
        public void CannotInstallInvalidPackage()
        {
            var home = Helpers.CreateTemporaryFolder("Home");
            string codebase = typeof(Program).GetTypeInfo().Assembly.Location;
            new DotnetNewCommand(_log, "-i", codebase)
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"{codebase} is not supported.");
        }



    }
}
