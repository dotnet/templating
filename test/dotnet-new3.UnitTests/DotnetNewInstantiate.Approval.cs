// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.TemplateEngine.TestHelper;
using VerifyXunit;
using Xunit;

namespace Dotnet_new3.IntegrationTests
{
    [UsesVerify]
    public partial class DotnetNewInstantiate
    {
        [Fact]
        public Task CanShowWarningIfPackageIsAvailableFromBuiltInSources()
        {
            string home = TestUtils.CreateTemporaryFolder("Home");
            string workingDirectory = TestUtils.CreateTemporaryFolder();
            new DotnetNewCommand(_log, "--install", "Microsoft.DotNet.Common.ItemTemplates::6.0.100", "--force")
                .WithCustomHive(home)
                .WithWorkingDirectory(workingDirectory)
                .Execute()
                .Should().Pass();

            var commandResult = new DotnetNewCommand(_log, "gitignore")
                  .WithCustomHive(home)
                  .WithWorkingDirectory(workingDirectory)
                  .Execute();

            commandResult
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr();

            return Verifier.Verify(commandResult.StdOut, _verifySettings)
            .AddScrubber(output =>
            {
                output.ScrubByRegex("'Microsoft\\.DotNet\\.Common\\.ItemTemplates::[A-Za-z0-9.-]+' is available in", "'Microsoft.DotNet.Common.ItemTemplates::%VERSION%' is available in");
            });
        }
    }
}
