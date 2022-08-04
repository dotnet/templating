﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.TemplateEngine.TestHelper;
using Xunit.Abstractions;

namespace Dotnet_new3.IntegrationTests
{
    [UsesVerify]
    [Collection("Verify Tests")]
    public class DotnetNew3CompleteTests
    {
        private readonly ITestOutputHelper _log;

        public DotnetNew3CompleteTests(ITestOutputHelper log)
        {
            _log = log;
        }

        [Fact]
        public Task CanDoTabCompletion()
        {
            string homeDir = TestUtils.CreateTemporaryFolder();
            var commandResult = new DotnetNewCommand(_log, "complete", $"new3 --debug:custom-hive {homeDir} ")
                .WithoutCustomHive()
                .Execute();

            commandResult
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr();

            return Verify(commandResult.StdOut);
        }

#pragma warning disable xUnit1004 // Test methods should not be skipped
        [Fact(Skip = "https://github.com/dotnet/command-line-api/issues/1519")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
        public void CanDoTabCompletionAtGivenPosition()
        {
            string homeDir = TestUtils.CreateTemporaryFolder();
            var commandResult = new DotnetNewCommand(_log, "complete", $"new3 co --debug:custom-hive {homeDir} --language C#", "--position", "7")
                .WithoutCustomHive()
                .Execute();

            commandResult
                .Should()
                .ExitWith(0)
                .And.NotHaveStdErr()
                .And.HaveStdOut("console");
        }
    }
}
