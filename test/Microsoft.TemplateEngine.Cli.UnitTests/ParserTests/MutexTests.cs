// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.Commands;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests.ParserTests
{
    public class MutexTests
    {
        [Fact]
        public async Task TestConcurrency()
        {
            ITemplateEngineHost host = TestHost.GetVirtualHost(additionalComponents: BuiltInTemplatePackagesProviderFactory.GetComponents(includeTestTemplates: false));
            TelemetryLogger telemetryLogger = new TelemetryLogger(null, false);
            NewCommandCallbacks callbacks = new NewCommandCallbacks();

            List<TestCommand> commands = new List<TestCommand>();
            List<Task<int>> tasks = new List<Task<int>>();

            for (int i = 0; i < 200; i++)
            {
                var command = new TestCommand(host, telemetryLogger, callbacks, $"command{i}");
                commands.Add(command);
                tasks.Add(command.InvokeAsync($"command{i}"));
            }

            foreach (TestCommand c in commands)
            {
                Thread.Sleep(100);
                c.CancelWait();
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach (var t in tasks)
            {
                Assert.True(t.IsCompletedSuccessfully);
                Assert.Equal((int)NewCommandStatus.Success, t.Result);
            }
        }
    }

    internal class TestCommand : BaseCommand<TestCommandArgs>
    {
        private bool _wait = true;

        public TestCommand(ITemplateEngineHost host, ITelemetryLogger logger, NewCommandCallbacks callbacks, string name, string description = null) : base(host, logger, callbacks, name, description) { }

        internal void CancelWait()
        {
            _wait = false;
        }

        protected override Task<NewCommandStatus> ExecuteAsync(TestCommandArgs args, IEngineEnvironmentSettings environmentSettings, InvocationContext context)
        {
            while (_wait)
            {
                Thread.Sleep(1);
            }
            return Task.FromResult(NewCommandStatus.Success);
        }

        protected override TestCommandArgs ParseContext(ParseResult parseResult) => new TestCommandArgs(this, parseResult);
    }

    internal class TestCommandArgs : GlobalArgs
    {
        public TestCommandArgs(BaseCommand command, ParseResult parseResult) : base(command, parseResult) { }
    }
}
