using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
            // Do actual install...
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
               .Should()
               .NotHaveStdOut()
               .And.HaveStdErrContaining("'BlaBlaBla' could not be installed, the package doesn't exist.");
        }

        [Fact]
        public void CannotInstallRemotePackageWithIncorrectVersion()
        {
            var home = Helpers.CreateTemporaryFolder("Home");

            new DotnetNewCommand(_log, "-i", "Microsoft.DotNet.Web.ProjectTemplates.5.0::16.0.0", "--quiet")
               .WithWorkingDirectory(Helpers.CreateTemporaryFolder())
               .WithEnvironmentVariable(Helpers.HomeEnvironmentVariableName, home)
               .Execute()
               .Should()
               .NotHaveStdOut()
               .And.HaveStdErrContaining("'Microsoft.DotNet.Web.ProjectTemplates.5.0::16.0.0' could not be installed, the package doesn't exist.");
        }
    }
}
