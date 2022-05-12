// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.Commands;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Mocks;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests.ParserTests
{
    public partial class InstantiateTests
    {
        [Fact]
        public async Task CanResolveTemplateToRun_WhenTheTemplateIsAllowed()
        {
            var template = new MockTemplateInfo(shortName: "test", identity: "testId1").WithConstraints(new TemplateConstraintInfo("test", "yes"));

            var templateGroup = TemplateGroup.FromTemplateList(CliTemplateInfo.FromTemplateInfo(new[] { template }, A.Fake<IHostSpecificDataLoader>())).Single();

            ITemplateEngineHost host = TestHost.GetVirtualHost(additionalComponents: new[] { (typeof(ITemplateConstraintFactory), (IIdentifiedComponent)new TestConstraintFactory("test")) });
            NewCommand myCommand = (NewCommand)NewCommandFactory.Create("new", _ => host, _ => new TelemetryLogger(null, false), new NewCommandCallbacks());
            IEngineEnvironmentSettings settings = new EngineEnvironmentSettings(host, virtualizeSettings: true);

            var parseResult = myCommand.Parse($"new test");
            InstantiateCommandArgs args = InstantiateCommandArgs.FromNewCommandArgs(new NewCommandArgs(myCommand, parseResult));
            var templateCommands = await InstantiateCommand.GetTemplateCommandAsync(args, settings, A.Fake<TemplatePackageManager>(), templateGroup, default);
            Assert.Equal(1, templateCommands.Count);
            Assert.Equal("testId1", templateCommands.Single().Template.Identity);
        }

        [Fact]
        public async Task CannotResolveTemplateToRun_WhenTheTemplateIsRestricted()
        {
            var template = new MockTemplateInfo(shortName: "test").WithConstraints(new TemplateConstraintInfo("test", "no"));

            var templateGroup = TemplateGroup.FromTemplateList(CliTemplateInfo.FromTemplateInfo(new[] { template }, A.Fake<IHostSpecificDataLoader>())).Single();

            ITemplateEngineHost host = TestHost.GetVirtualHost(additionalComponents: new[] { (typeof(ITemplateConstraintFactory), (IIdentifiedComponent)new TestConstraintFactory("test")) });
            NewCommand myCommand = (NewCommand)NewCommandFactory.Create("new", _ => host, _ => new TelemetryLogger(null, false), new NewCommandCallbacks());
            IEngineEnvironmentSettings settings = new EngineEnvironmentSettings(host, virtualizeSettings: true);

            var parseResult = myCommand.Parse($"new test");
            InstantiateCommandArgs args = InstantiateCommandArgs.FromNewCommandArgs(new NewCommandArgs(myCommand, parseResult));
            var templateCommands = await InstantiateCommand.GetTemplateCommandAsync(args, settings, A.Fake<TemplatePackageManager>(), templateGroup, default);
            Assert.Equal(0, templateCommands.Count);
        }

        [Fact]
        public async Task CannotResolveTemplateToRun_WhenTheConstraintCannotBeEvaluated()
        {
            var template = new MockTemplateInfo(shortName: "test").WithConstraints(new TemplateConstraintInfo("test", "bad-arg"));

            var templateGroup = TemplateGroup.FromTemplateList(CliTemplateInfo.FromTemplateInfo(new[] { template }, A.Fake<IHostSpecificDataLoader>())).Single();

            ITemplateEngineHost host = TestHost.GetVirtualHost(additionalComponents: new[] { (typeof(ITemplateConstraintFactory), (IIdentifiedComponent)new TestConstraintFactory("test")) });
            NewCommand myCommand = (NewCommand)NewCommandFactory.Create("new", _ => host, _ => new TelemetryLogger(null, false), new NewCommandCallbacks());
            IEngineEnvironmentSettings settings = new EngineEnvironmentSettings(host, virtualizeSettings: true);

            var parseResult = myCommand.Parse($"new test");
            InstantiateCommandArgs args = InstantiateCommandArgs.FromNewCommandArgs(new NewCommandArgs(myCommand, parseResult));
            var templateCommands = await InstantiateCommand.GetTemplateCommandAsync(args, settings, A.Fake<TemplatePackageManager>(), templateGroup, default);
            Assert.Equal(0, templateCommands.Count);
        }
    }
}

