// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET

using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Authoring.TemplateApiVerifier;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;
using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateEngine.Tests;
using Xunit.Abstractions;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.UnitTests
{
    [Collection("Verify Tests")]
    public class SnapshotTests : TestBase
    {
        private readonly ILogger _log;

        public SnapshotTests(ITestOutputHelper log)
        {
            _log = new XunitLoggerProvider(log).CreateLogger("TestRun");
        }

        [Fact]
        public Task TestGeneratedSymbolWithRefToDerivedSymbol()
        {
            string templateLocation = GetTestTemplateLocation("TemplateWithGeneratedSymbolWithRefToDerivedSymbol");
            var templateParams = new Dictionary<string, string?>()
            {
                { "NugetToolName", "nuget" }
            };
            string workingDir = TestUtils.CreateTemporaryFolder();

            TemplateVerifierOptions options =
                new TemplateVerifierOptions(templateName: "TestAssets.TemplateGenSymWithRefToDerivedSym")
                {
                    TemplatePath = templateLocation,
                    OutputDirectory = workingDir,
                    DoNotAppendTemplateArgsToScenarioName = true,
                    DoNotPrependTemplateNameToScenarioName = true,
                    SnapshotsDirectory = "Approvals"
                }
                .WithInstantiationThroughTemplateCreatorApi(templateParams);

            VerificationEngine engine = new VerificationEngine(_log);
            return engine.Execute(options);
        }

        [Fact]
        public Task TestGeneratedSymbolWithRefToDerivedSymbol_DifferentOrder()
        {
            string templateLocation = GetTestTemplateLocation("TemplateWithGenSymbolWithRefToDerivedSymbol_DifferentOrder");
            var templateParams = new Dictionary<string, string?>()
            {
                { "NugetToolName", "nuget" }
            };
            string workingDir = TestUtils.CreateTemporaryFolder();

            TemplateVerifierOptions options =
                new TemplateVerifierOptions(templateName: "TestAssets.TemplateGenSymWithRefToDerivedSym_DiffOrder")
                {
                    TemplatePath = templateLocation,
                    OutputDirectory = workingDir,
                    DoNotAppendTemplateArgsToScenarioName = true,
                    DoNotPrependTemplateNameToScenarioName = true,
                    SnapshotsDirectory = "Approvals"
                }
                .WithInstantiationThroughTemplateCreatorApi(templateParams);

            VerificationEngine engine = new VerificationEngine(_log);
            return engine.Execute(options);
        }

        [Fact]
        public Task TestComputedSymbolDependsOnGeneratedSymbol()
        {
            string templateLocation = GetTestTemplateLocation("TemplateWithComputedSymbolDependsOnGeneratedSymbol");
            var templateParams = new Dictionary<string, string?>()
            {
                { "Connection", "vpn" },
                { "ActionOption", "recommended" }
            };
            string workingDir = TestUtils.CreateTemporaryFolder();

            TemplateVerifierOptions options =
                new TemplateVerifierOptions(templateName: "TestAssets.TemplateWithComSymDependsOnGenSym")
                {
                    TemplatePath = templateLocation,
                    OutputDirectory = workingDir,
                    DoNotAppendTemplateArgsToScenarioName = true,
                    DoNotPrependTemplateNameToScenarioName = true,
                    SnapshotsDirectory = "Approvals"
                }
                .WithInstantiationThroughTemplateCreatorApi(templateParams);

            VerificationEngine engine = new VerificationEngine(_log);
            return engine.Execute(options);
        }

        [Fact]
        public Task TestComputedSymbolDependsOnGeneratedSymbol_DifferentOrder()
        {
            string templateLocation = GetTestTemplateLocation("TemplateWithComputedSymbolDependsOnGeneratedSymbol_DifferentOrder");
            var templateParams = new Dictionary<string, string?>()
            {
                { "Connection", "vpn" },
                { "ActionOption", "recommended" }
            };
            string workingDir = TestUtils.CreateTemporaryFolder();

            TemplateVerifierOptions options =
                new TemplateVerifierOptions(templateName: "TestAssets.TemplateWithComSymDependsOnGenSym_DiffOrder")
                {
                    TemplatePath = templateLocation,
                    OutputDirectory = workingDir,
                    DoNotAppendTemplateArgsToScenarioName = true,
                    DoNotPrependTemplateNameToScenarioName = true,
                    SnapshotsDirectory = "Approvals"
                }
                .WithInstantiationThroughTemplateCreatorApi(templateParams);

            VerificationEngine engine = new VerificationEngine(_log);
            return engine.Execute(options);
        }

        [Fact]
        public Task TestComputedSymbolUsedInDerivedSymbol_InvalidConfiguration()
        {
            string templateLocation = GetTestTemplateLocation("TemplateWithComputedSymbolInDerivedSymbol");
            var templateParams = new Dictionary<string, string?>()
            {
                { "firstName", "Will" },
                { "lastName", "Smith" }
            };
            string workingDir = TestUtils.CreateTemporaryFolder();

            TemplateVerifierOptions options =
                new TemplateVerifierOptions(templateName: "TestAssets.TemplateWithComSymInDerSym")
                {
                    TemplatePath = templateLocation,
                    OutputDirectory = workingDir,
                    DoNotAppendTemplateArgsToScenarioName = true,
                    DoNotPrependTemplateNameToScenarioName = true,
                    SnapshotsDirectory = "Approvals"
                }
                .WithInstantiationThroughTemplateCreatorApi(templateParams);

            VerificationEngine engine = new VerificationEngine(_log);
            return engine.Execute(options);
        }

        [Fact]
        public Task TestGeneratedSymbolUsedInDerivedSymbol_InvalidConfiguration()
        {
            string templateLocation = GetTestTemplateLocation("TemplateWithGeneratedSymbolInDerivedSymbol");
            string workingDir = TestUtils.CreateTemporaryFolder();

            TemplateVerifierOptions options =
                new TemplateVerifierOptions(templateName: "TestAssets.TemplateWithGenSymInDerSym")
                {
                    TemplatePath = templateLocation,
                    OutputDirectory = workingDir,
                    DoNotAppendTemplateArgsToScenarioName = true,
                    DoNotPrependTemplateNameToScenarioName = true,
                    SnapshotsDirectory = "Approvals"
                }
                .WithInstantiationThroughTemplateCreatorApi(new Dictionary<string, string?>()
                {
                    { "firstName", "pEtRo" }
                });

            VerificationEngine engine = new VerificationEngine(_log);
            return engine.Execute(options);
        }

        [Fact]
        public Task TestDerivedSymbolUsedInComputedSymbol()
        {
            string templateLocation = GetTestTemplateLocation("TemplateWithDerivedSymbolInComputedSymbol");
            var templateParams = new Dictionary<string, string?>()
            {
                { "firstName", "will" },
                { "lastName", "smith" }
            };
            string workingDir = TestUtils.CreateTemporaryFolder();

            TemplateVerifierOptions options =
                new TemplateVerifierOptions(templateName: "TestAssets.TemplateWithDerSymInComSym")
                {
                    TemplatePath = templateLocation,
                    OutputDirectory = workingDir,
                    DoNotAppendTemplateArgsToScenarioName = true,
                    DoNotPrependTemplateNameToScenarioName = true,
                    SnapshotsDirectory = "Approvals"
                }
                .WithInstantiationThroughTemplateCreatorApi(templateParams);

            VerificationEngine engine = new VerificationEngine(_log);
            return engine.Execute(options);
        }

        [Fact]
        public Task TestDerivedSymbolUsedInComputedSymbol_DifferentOrder()
        {
            string templateLocation = GetTestTemplateLocation("TemplateWithDerivedSymbolInComputedSymbol_DifferentOrder");
            var templateParams = new Dictionary<string, string?>()
            {
                { "firstName", "Will" },
                { "lastName", "smith" }
            };
            string workingDir = TestUtils.CreateTemporaryFolder();

            TemplateVerifierOptions options =
                new TemplateVerifierOptions(templateName: "TestAssets.TemplateWithDerSymInComSym_DiffOrder")
                {
                    TemplatePath = templateLocation,
                    OutputDirectory = workingDir,
                    DoNotAppendTemplateArgsToScenarioName = true,
                    DoNotPrependTemplateNameToScenarioName = true,
                    SnapshotsDirectory = "Approvals"
                }
                .WithInstantiationThroughTemplateCreatorApi(templateParams);

            VerificationEngine engine = new VerificationEngine(_log);
            return engine.Execute(options);
        }
    }
}

#endif
