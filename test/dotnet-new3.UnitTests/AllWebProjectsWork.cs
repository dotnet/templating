// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.NET.TestFramework.Assertions;
using Microsoft.NET.TestFramework.Commands;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;
using Xunit.Abstractions;

namespace Dotnet_new3.IntegrationTests
{
    public class AllWebProjectsWork : IClassFixture<AllProjectsWorkFixture>
    {
        private readonly AllProjectsWorkFixture _fixture;
        private readonly ITestOutputHelper _log;

        public AllWebProjectsWork(AllProjectsWorkFixture fixture, ITestOutputHelper log)
        {
            _fixture = fixture;
            _log = log;
        }

        [Theory]
        [InlineData("emptyweb_cs-60", new[] { "web" })]
        [InlineData("mvc_cs-60", new[] { "mvc" })]
        //https://github.com/dotnet/aspnetcore/issues/44729
        //[InlineData("mvc_fs-60", "mvc", "-lang", "F#")]
        //avoid build to restore outdated packages
        [InlineData("api_cs-60", new[] { "webapi", "--no-restore" }, false)]
        [InlineData("emptyweb_cs-31", new[] { "web", "-f", "netcoreapp3.1" })]
        [InlineData("mvc_cs-31", new[] { "mvc", "-f", "netcoreapp3.1" })]
        [InlineData("mvc_fs-31", new[] { "mvc", "-lang", "F#", "-f", "netcoreapp3.1" })]
        [InlineData("api_cs-31", new[] { "webapi", "-f", "netcoreapp3.1" })]
        public void AllWebProjectsRestoreAndBuild(string testName, string[] args, bool build = true)
        {
            string workingDir = Path.Combine(_fixture.BaseWorkingDirectory, testName);
            Directory.CreateDirectory(workingDir);

            new DotnetNewCommand(_log, args)
                .WithCustomHive(_fixture.HomeDirectory)
                .WithWorkingDirectory(workingDir)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr();

            if (build)
            {
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
            }

            Directory.Delete(workingDir, true);
        }
    }

    public sealed class AllProjectsWorkFixture : SharedHomeDirectory
    {
        public AllProjectsWorkFixture(IMessageSink messageSink) : base(messageSink)
        {
            BaseWorkingDirectory = TestUtils.CreateTemporaryFolder(nameof(AllWebProjectsWork));
            // create nuget.config file with nuget.org listed
            new DotnetNewCommand(Log, "nugetconfig")
                .WithCustomHive(HomeDirectory)
                .WithWorkingDirectory(BaseWorkingDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr();

            InstallPackage("Microsoft.DotNet.Web.ProjectTemplates.6.0", BaseWorkingDirectory);
            InstallPackage("Microsoft.DotNet.Web.ProjectTemplates.3.1", BaseWorkingDirectory);
        }

        internal string BaseWorkingDirectory { get; private set; }
    }
}
