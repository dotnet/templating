// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.NET.TestFramework.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace dotnet_new3.UnitTests
{
    public class DotnetNewUninstall
    {
        private readonly ITestOutputHelper _log;

        public DotnetNewUninstall(ITestOutputHelper log)
        {
            _log = log;
        }

        [Fact]
        public void CanListInstalledSources_Folder()
        {
            string home = Helpers.CreateTemporaryFolder("Home");
            string workingDirectory = Helpers.CreateTemporaryFolder();
            Helpers.InstallTestTemplate("TemplateResolution/DifferentLanguagesGroup/BasicFSharp", _log, workingDirectory, home);

            new DotnetNewCommand(_log, "-u")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining($"TemplateResolution{Path.DirectorySeparatorChar}DifferentLanguagesGroup{Path.DirectorySeparatorChar}BasicFSharp")
                .And.HaveStdOutMatching($"^\\s*dotnet new3 -u .*TemplateResolution{Regex.Escape(Path.DirectorySeparatorChar.ToString())}DifferentLanguagesGroup{Regex.Escape(Path.DirectorySeparatorChar.ToString())}BasicFSharp$", RegexOptions.Multiline);
        }

        [Fact]
        public void CanListInstalledSources_NuGet()
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
                .And.HaveStdOutContaining("web")
                .And.HaveStdOutContaining("blazorwasm");

            new DotnetNewCommand(_log, "-u")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining("Microsoft.DotNet.Web.ProjectTemplates.5.0")
                .And.HaveStdOutContaining("Version: 5.0.0")
                .And.HaveStdOutContaining("Author: Microsoft")
                .And.HaveStdOutContaining("dotnet new3 -u Microsoft.DotNet.Web.ProjectTemplates.5.0");
        }

        [Fact]
        public void CanListInstalledSources_WhenNothingIsInstalled()
        {
            var home = Helpers.CreateTemporaryFolder("Home");
            new DotnetNewCommand(_log, "-u", "--quiet")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOut($"Currently installed items:{Environment.NewLine}(No Items)");
        }

        [Fact]
        public void CanUninstall_Folder()
        {
            string home = Helpers.CreateTemporaryFolder("Home");
            string workingDirectory = Helpers.CreateTemporaryFolder();
            string templateLocation = Helpers.InstallTestTemplate("TemplateResolution/DifferentLanguagesGroup/BasicFSharp", _log, workingDirectory, home);

            new DotnetNewCommand(_log, "-u")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining($"TemplateResolution{Path.DirectorySeparatorChar}DifferentLanguagesGroup{Path.DirectorySeparatorChar}BasicFSharp")
                .And.HaveStdOutMatching($"^\\s*dotnet new3 -u .*TemplateResolution{Regex.Escape(Path.DirectorySeparatorChar.ToString())}DifferentLanguagesGroup{Regex.Escape(Path.DirectorySeparatorChar.ToString())}BasicFSharp$", RegexOptions.Multiline);

            new DotnetNewCommand(_log, "-u", templateLocation)
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOut($"The template source {templateLocation} was successfully uninstalled.");

            new DotnetNewCommand(_log, "-u")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOut($"Currently installed items:{Environment.NewLine}(No Items)");

            Assert.True(Directory.Exists(templateLocation));
        }

        [Fact]
        public void CanUninstall_NuGet()
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
                .And.HaveStdOutContaining("web")
                .And.HaveStdOutContaining("blazorwasm");

            new DotnetNewCommand(_log, "-u")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining("Microsoft.DotNet.Web.ProjectTemplates.5.0")
                .And.HaveStdOutContaining("Version: 5.0.0")
                .And.HaveStdOutContaining("Author: Microsoft")
                .And.HaveStdOutContaining("dotnet new3 -u Microsoft.DotNet.Web.ProjectTemplates.5.0");

            Assert.True(File.Exists(Path.Combine(home, ".templateengine", "packages", "Microsoft.DotNet.Web.ProjectTemplates.5.0.5.0.0.nupkg")));

            new DotnetNewCommand(_log, "-u", "Microsoft.DotNet.Web.ProjectTemplates.5.0")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOut($"The template source Microsoft.DotNet.Web.ProjectTemplates.5.0::5.0.0 was successfully uninstalled.");

            new DotnetNewCommand(_log, "-u")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOut($"Currently installed items:{Environment.NewLine}(No Items)");

            Assert.False(File.Exists(Path.Combine(home, ".templateengine", "packages", "Microsoft.DotNet.Web.ProjectTemplates.5.0.5.0.0.nupkg")));
        }

        [Fact]
        public void CanUninstallSeveralSources()
        {
            var home = Helpers.CreateTemporaryFolder("Home");
            string workingDirectory = Helpers.CreateTemporaryFolder();
            string basicFSharp = Helpers.InstallTestTemplate("TemplateResolution/DifferentLanguagesGroup/BasicFSharp", _log, workingDirectory, home);
            string basicVB = Helpers.InstallTestTemplate("TemplateResolution/DifferentLanguagesGroup/BasicVB", _log, workingDirectory, home);

            new DotnetNewCommand(_log, "-i", "Microsoft.DotNet.Web.ProjectTemplates.5.0", "-i", "Microsoft.DotNet.Common.ProjectTemplates.5.0", "--quiet")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining("web")
                .And.HaveStdOutContaining("blazorwasm")
                .And.HaveStdOutContaining("console")
                .And.HaveStdOutContaining("classlib");

            new DotnetNewCommand(_log, "-u", "Microsoft.DotNet.Common.ProjectTemplates.5.0", "-u", basicFSharp)
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutMatching($"^The template source Microsoft\\.DotNet\\.Common\\.ProjectTemplates\\.5\\.0::([\\d\\.a-z-])+ was successfully uninstalled\\.\\s*$", RegexOptions.Multiline)
                .And.HaveStdOutContaining($"The template source {basicFSharp} was successfully uninstalled.");

            new DotnetNewCommand(_log, "-u")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining("Microsoft.DotNet.Web.ProjectTemplates.5.0")
                .And.HaveStdOutContaining(basicVB)
                .And.NotHaveStdOutContaining("Microsoft.DotNet.Common.ProjectTemplates.5.0")
                .And.NotHaveStdOutContaining(basicFSharp);


        }
    }
}
