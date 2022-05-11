// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
using Microsoft.TemplateEngine.Mocks;
using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateEngine.Utils;
using Xunit;

namespace Microsoft.TemplateEngine.Core.UnitTests
{
    public class OrchestratorTests : IClassFixture<LoggerHelper>
    {
        private ILogger _logger;

        public OrchestratorTests(LoggerHelper loggerHelper)
        {
            _logger = loggerHelper.CreateLogger();
        }

        [Fact(DisplayName = nameof(VerifyRun))]
        public void VerifyRun()
        {
            MockFileSystem fileSystem = new MockFileSystem();
            Util.Orchestrator orchestrator = new Util.Orchestrator(_logger, new MockFileSystem());
            //mnt.MockRoot.AddDirectory("subdir").AddFile("test.file", System.Array.Empty<byte>());
            fileSystem.Add("subdir/test.file", string.Empty);
            orchestrator.Run(new MockGlobalRunSpec(),  "/", @"c:\temp");
        }
    }
}
