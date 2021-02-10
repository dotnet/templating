// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.NET.TestFramework.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace dotnet_new3.UnitTests
{
    public class DotnetNewUpdateApply
    {
        private readonly ITestOutputHelper _log;

        public DotnetNewUpdateApply(ITestOutputHelper log)
        {
            _log = log;
        }

        [Fact]
        public void CanApplyUpdates()
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
                .And.NotHaveStdOutContaining("Determining projects to restore...")
                .And.HaveStdOutContaining("console")
                .And.HaveStdOutContaining("classlib");

            new DotnetNewCommand(_log, "--update-check")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining("An update for template pack Microsoft.DotNet.Common.ProjectTemplates.5.0::5.0.0 is available.");

            new DotnetNewCommand(_log, "--update-apply")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutMatching($"^The template source Microsoft\\.DotNet\\.Common\\.ProjectTemplates\\.5\\.0::([\\d\\.a-z-])+ was successfully installed\\.\\s*$", System.Text.RegularExpressions.RegexOptions.Multiline)
                .And.HaveStdOutContaining("console")
                .And.HaveStdOutContaining("Console Application");
        }

        [Fact]
        public void DoesNotApplyUpdatesWhenAllTemplatesAreUpToDate()
        {
            var home = Helpers.CreateTemporaryFolder("Home");
            string workingDirectory = Helpers.CreateTemporaryFolder();
            string templateLocation = Helpers.InstallTestTemplate("TemplateResolution/DifferentLanguagesGroup/BasicFSharp", _log, workingDirectory, home);
            new DotnetNewCommand(_log, "-i", "Microsoft.DotNet.Common.ProjectTemplates.5.0", "--quiet")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.NotHaveStdOutContaining("Determining projects to restore...")
                .And.HaveStdOutContaining("console")
                .And.HaveStdOutContaining("classlib");

            new DotnetNewCommand(_log, "--update-apply")
                .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
                .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOut("All template sources are up-to-date.");
        }
    }
}
