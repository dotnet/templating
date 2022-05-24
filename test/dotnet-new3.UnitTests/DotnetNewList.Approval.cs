﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.TemplateEngine.TestHelper;
using VerifyXunit;
using Xunit;

namespace Dotnet_new3.IntegrationTests
{
    public partial class DotnetNewList
    {
        [Theory]
        [InlineData("-l")]
        [InlineData("--list")]
        public Task CanShowDeprecationMessage_WhenLegacyCommandIsUsed(string commandName)
        {
            var commandResult = new DotnetNewCommand(_log, commandName)
                .WithCustomHive()
                .WithWorkingDirectory(TestUtils.CreateTemporaryFolder())
                .Execute();

            commandResult
                .Should()
                .Pass();

            return Verifier.Verify(commandResult.StdOut, _verifySettings)
                .UseTextForParameters("common")
                .DisableRequireUniquePrefix();
        }

        [Fact]
        public Task DoNotShowDeprecationMessage_WhenNewCommandIsUsed()
        {
            var commandResult = new DotnetNewCommand(_log, "list")
                .WithCustomHive()
                .WithWorkingDirectory(TestUtils.CreateTemporaryFolder())
                .Execute();

            commandResult
                .Should()
                .Pass();

            return Verifier.Verify(commandResult.StdOut, _verifySettings);
        }
    }
}
