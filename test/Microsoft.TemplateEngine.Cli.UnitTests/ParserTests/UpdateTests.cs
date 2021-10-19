﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.Commands;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests.ParserTests
{
    public class UpdateTests
    {
        [Theory]
        [InlineData("--add-source")]
        [InlineData("--nuget-source")]
        public void Update_CanParseAddSourceOption(string optionName)
        {
            ITemplateEngineHost host = TestHost.GetVirtualHost(additionalComponents: BuiltInTemplatePackagesProviderFactory.GetComponents(includeTestTemplates: false));
            NewCommand myCommand = (NewCommand)NewCommandFactory.Create("new", host, new TelemetryLogger(null, false), new NewCommandCallbacks());

            var parseResult = myCommand.Parse($"new update {optionName} my-custom-source");
            UpdateCommandArgs args = new UpdateCommandArgs((UpdateCommand)parseResult.CommandResult.Command, parseResult);

            Assert.Single(args.AdditionalSources);
            Assert.Contains("my-custom-source", args.AdditionalSources);
        }

        [Theory]
        [InlineData("--update-apply")]
        [InlineData("--update-check")]
        [InlineData("update")]
        public void Update_Error_WhenArguments(string commandName)
        {
            ITemplateEngineHost host = TestHost.GetVirtualHost(additionalComponents: BuiltInTemplatePackagesProviderFactory.GetComponents(includeTestTemplates: false));
            NewCommand myCommand = (NewCommand)NewCommandFactory.Create("new", host, new TelemetryLogger(null, false), new NewCommandCallbacks());

            var parseResult = myCommand.Parse($"new {commandName} source");

            Assert.True(parseResult.Errors.Any());
            Assert.Contains(parseResult.Errors, error => error.Message.Contains("Unrecognized command or argument 'source'"));
        }

        [Theory]
        [InlineData("new update --add-source my-custom-source1 my-custom-source2")]
        [InlineData("new update --check-only --add-source my-custom-source1 --add-source my-custom-source2")]
        public void Update_CanParseAddSourceOption_MultipleEntries(string testCase)
        {
            ITemplateEngineHost host = TestHost.GetVirtualHost(additionalComponents: BuiltInTemplatePackagesProviderFactory.GetComponents(includeTestTemplates: false));
            NewCommand myCommand = (NewCommand)NewCommandFactory.Create("new", host, new TelemetryLogger(null, false), new NewCommandCallbacks());
            var parseResult = myCommand.Parse(testCase);
            UpdateCommandArgs args = new UpdateCommandArgs((UpdateCommand)parseResult.CommandResult.Command, parseResult);

            Assert.Equal(2, args.AdditionalSources.Count);
            Assert.Contains("my-custom-source1", args.AdditionalSources);
            Assert.Contains("my-custom-source2", args.AdditionalSources);
        }

        [Fact]
        public void Update_CanParseInteractiveOption()
        {
            ITemplateEngineHost host = TestHost.GetVirtualHost(additionalComponents: BuiltInTemplatePackagesProviderFactory.GetComponents(includeTestTemplates: false));
            NewCommand myCommand = (NewCommand)NewCommandFactory.Create("new", host, new TelemetryLogger(null, false), new NewCommandCallbacks());

            var parseResult = myCommand.Parse($"new update --interactive");
            UpdateCommandArgs args = new UpdateCommandArgs((UpdateCommand)parseResult.CommandResult.Command, parseResult);

            Assert.True(args.Interactive);

            parseResult = myCommand.Parse($"new update");
            args = new UpdateCommandArgs((UpdateCommand)parseResult.CommandResult.Command, parseResult);

            Assert.False(args.Interactive);
        }

        [Fact]
        public void Update_CanParseCheckOnlyOption()
        {
            ITemplateEngineHost host = TestHost.GetVirtualHost(additionalComponents: BuiltInTemplatePackagesProviderFactory.GetComponents(includeTestTemplates: false));
            NewCommand myCommand = (NewCommand)NewCommandFactory.Create("new", host, new TelemetryLogger(null, false), new NewCommandCallbacks());

            var parseResult = myCommand.Parse($"new update --check-only");
            UpdateCommandArgs args = new UpdateCommandArgs((UpdateCommand)parseResult.CommandResult.Command, parseResult);

            Assert.True(args.CheckOnly);

            parseResult = myCommand.Parse($"new update");
            args = new UpdateCommandArgs((UpdateCommand)parseResult.CommandResult.Command, parseResult);

            Assert.False(args.CheckOnly);
        }

        [Fact]
        public void Update_Legacy_CanParseCheckOnlyOption()
        {
            ITemplateEngineHost host = TestHost.GetVirtualHost(additionalComponents: BuiltInTemplatePackagesProviderFactory.GetComponents(includeTestTemplates: false));
            NewCommand myCommand = (NewCommand)NewCommandFactory.Create("new", host, new TelemetryLogger(null, false), new NewCommandCallbacks());

            var parseResult = myCommand.Parse($"new --update-check");
            UpdateCommandArgs args = new UpdateCommandArgs((LegacyUpdateCheckCommand)parseResult.CommandResult.Command, parseResult);

            Assert.True(args.CheckOnly);

            parseResult = myCommand.Parse($"new --update-apply");
            args = new UpdateCommandArgs((LegacyUpdateApplyCommand)parseResult.CommandResult.Command, parseResult);

            Assert.False(args.CheckOnly);
        }

        [Theory]
        [InlineData("new --update-check --add-source my-custom-source")]
        [InlineData("new --update-apply --nuget-source my-custom-source")]
        [InlineData("new --nuget-source my-custom-source --update-apply")]
        public void Update_Legacy_CanParseAddSourceOption(string testCase)
        {
            ITemplateEngineHost host = TestHost.GetVirtualHost(additionalComponents: BuiltInTemplatePackagesProviderFactory.GetComponents(includeTestTemplates: false));
            NewCommand myCommand = (NewCommand)NewCommandFactory.Create("new", host, new TelemetryLogger(null, false), new NewCommandCallbacks());

            var parseResult = myCommand.Parse(testCase);
            UpdateCommandArgs args = new UpdateCommandArgs((BaseUpdateCommand)parseResult.CommandResult.Command, parseResult);

            Assert.Single(args.AdditionalSources);
            Assert.Contains("my-custom-source", args.AdditionalSources);
        }

        [Theory]
        [InlineData("new --update-check source --interactive")]
        [InlineData("new --interactive --update-apply source")]
        public void Update_Legacy_CanParseInteractiveOption(string testCase)
        {
            ITemplateEngineHost host = TestHost.GetVirtualHost(additionalComponents: BuiltInTemplatePackagesProviderFactory.GetComponents(includeTestTemplates: false));
            NewCommand myCommand = (NewCommand)NewCommandFactory.Create("new", host, new TelemetryLogger(null, false), new NewCommandCallbacks());

            var parseResult = myCommand.Parse(testCase);
            UpdateCommandArgs args = new UpdateCommandArgs((BaseUpdateCommand)parseResult.CommandResult.Command, parseResult);

            Assert.True(args.Interactive);
        }

        [Theory]
        [InlineData("new --update-check --add-source my-custom-source1 --add-source my-custom-source2")]
        [InlineData("new --add-source my-custom-source1 --add-source my-custom-source2 --update-apply source")]
        [InlineData("new --add-source my-custom-source1 --update-apply --add-source my-custom-source2")]
        public void Update_Legacy_CanParseAddSourceOption_MultipleEntries(string testCase)
        {
            ITemplateEngineHost host = TestHost.GetVirtualHost(additionalComponents: BuiltInTemplatePackagesProviderFactory.GetComponents(includeTestTemplates: false));
            NewCommand myCommand = (NewCommand)NewCommandFactory.Create("new", host, new TelemetryLogger(null, false), new NewCommandCallbacks());
            var parseResult = myCommand.Parse(testCase);
            UpdateCommandArgs args = new UpdateCommandArgs((BaseUpdateCommand)parseResult.CommandResult.Command, parseResult);

            Assert.Equal(2, args.AdditionalSources.Count);
            Assert.Contains("my-custom-source1", args.AdditionalSources);
            Assert.Contains("my-custom-source2", args.AdditionalSources);
        }

    }
}
