// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.NET.TestFramework.Assertions;
using System.IO;
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
        public void CanInstallNupkgFromRemoteSource()
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
                .And.HaveStdOutContaining("web")
                .And.HaveStdOutContaining("blazorwasm");
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
               .And.HaveStdErrContaining("'BlaBlaBla' could not be installed, the package does not exist.");
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
               .And.HaveStdErrContaining("'Microsoft.DotNet.Web.ProjectTemplates.5.0::16.0.0' could not be installed, the package does not exist.");
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
                 .And.HaveStdErrContaining("The template source Microsoft.DotNet.Common.ProjectTemplates.5.0::5.0.0 is already installed.");
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
                 .And.HaveStdErrContaining($"The template source {basicFSharp} is already installed.");
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
                .And.HaveStdOutMatching($"The template source Microsoft\\.TemplateEngine\\.TestTemplates::([\\d\\.a-z-])+ was successfully installed\\.")
                .And.HaveStdOutContaining("TestAssets.TemplateWithTags")
                .And.HaveStdOutContaining("TestAssets.ConfigurationKitchenSink");
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
                .And.HaveStdOutMatching($"The template source Microsoft\\.DotNet\\.Common\\.ProjectTemplates\\.5\\.0::([\\d\\.a-z-])+ was successfully installed\\.")
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
                .And.HaveStdOutContaining($"The template source Microsoft.DotNet.Common.ProjectTemplates.5.0::5.0.0 was successfully installed.")
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


    }
}
