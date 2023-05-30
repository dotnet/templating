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
        public Task TestTemplateWithGeneratedInComputed()
        {
            string templateLocation = GetTestTemplateLocation("TemplateWithGeneratedInComputed");

            string workingDir = TestUtils.CreateTemporaryFolder();

            var templateParams = new Dictionary<string, string?>()
            {
                { "dependencyInjection", "true" },
                { "navigation", "regions" }
            };

            TemplateVerifierOptions options =
                new TemplateVerifierOptions(templateName: "TestAssets.TemplateWithGeneratedInComputed")
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
        public Task TestTemplateWithGeneratedSwitchInComputed()
        {
            string templateLocation = GetTestTemplateLocation("TemplateWithGeneratedSwitchInComputed");

            string workingDir = TestUtils.CreateTemporaryFolder();

            var templateParams = new Dictionary<string, string?>()
            {
                { "dependencyInjection", "true" },
                { "navigation", "regions" }
            };

            TemplateVerifierOptions options =
                new TemplateVerifierOptions(templateName: "TestAssets.TemplateWithGeneratedSwitchInComputed")
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
        public Task TestTemplateWithComputedInGenerated()
        {
            string templateLocation = GetTestTemplateLocation("TemplateWithComputedInGenerated");

            TemplateVerifierOptions options =
                new TemplateVerifierOptions(templateName: "TestAssets.TemplateWithComputedInGenerated")
                {
                    TemplatePath = templateLocation,
                    OutputDirectory = TestUtils.CreateTemporaryFolder(),
                    DoNotAppendTemplateArgsToScenarioName = true,
                    DoNotPrependTemplateNameToScenarioName = true,
                    SnapshotsDirectory = "Approvals"
                }
                .WithInstantiationThroughTemplateCreatorApi(new Dictionary<string, string?>());

            VerificationEngine engine = new VerificationEngine(_log);
            return engine.Execute(options);
        }

        [Fact]
        public Task TestTemplateWithComputedInDerivedThroughGenerated()
        {
            string templateLocation = GetTestTemplateLocation("TemplateWithComputedInDerivedThroughGenerated");

            TemplateVerifierOptions options =
                new TemplateVerifierOptions(templateName: "TestAssets.TemplateWithComputedInDerivedThroughGenerated")
                {
                    TemplatePath = templateLocation,
                    OutputDirectory = TestUtils.CreateTemporaryFolder(),
                    DoNotAppendTemplateArgsToScenarioName = true,
                    DoNotPrependTemplateNameToScenarioName = true,
                    SnapshotsDirectory = "Approvals"
                }
                .WithInstantiationThroughTemplateCreatorApi(new Dictionary<string, string?>());

            VerificationEngine engine = new VerificationEngine(_log);
            return engine.Execute(options);
        }

        [Fact]
        public Task TestTemplateWithBrokenGeneratedInComputed()
        {
            string templateLocation = GetTestTemplateLocation("TemplateWithBrokenGeneratedInComputed");
            var templateParams = new Dictionary<string, string?>()
            {
                { "dependencyInjection", "true" }
            };

            TemplateVerifierOptions options =
                new TemplateVerifierOptions(templateName: "TestAssets.TemplateWithBrokenGeneratedInComputed")
                {
                    TemplatePath = templateLocation,
                    OutputDirectory = TestUtils.CreateTemporaryFolder(),
                    DoNotAppendTemplateArgsToScenarioName = true,
                    DoNotPrependTemplateNameToScenarioName = true,
                    SnapshotsDirectory = "Approvals",
                    VerifyCommandOutput = true
                }
                .WithInstantiationThroughTemplateCreatorApi(templateParams);

            return new VerificationEngine(_log)
                .Execute(options);
        }

        [Fact]
        public Task TestTemplateWithVariablesInGeneratedThatUsedInComputed()
        {
            string templateLocation = GetTestTemplateLocation("TemplateWithBrokenGeneratedInComputed");

            TemplateVerifierOptions options =
                new TemplateVerifierOptions(templateName: "TestAssets.TemplateWithBrokenGeneratedInComputed")
                {
                    TemplatePath = templateLocation,
                    OutputDirectory = TestUtils.CreateTemporaryFolder(),
                    DoNotAppendTemplateArgsToScenarioName = true,
                    DoNotPrependTemplateNameToScenarioName = true,
                    SnapshotsDirectory = "Approvals",
                    VerifyCommandOutput = true
                }
                .WithInstantiationThroughTemplateCreatorApi(new Dictionary<string, string?>());

            return new VerificationEngine(_log)
                .Execute(options);
        }

        [Fact]
        public Task TestTemplateWithCircleDependencyInMacros()
        {
            string templateLocation = GetTestTemplateLocation("TemplateWithCircleDependencyInMacros");

            TemplateVerifierOptions options =
                new TemplateVerifierOptions(templateName: "TestAssets.TemplateWithCircleDependencyInMacros")
                {
                    TemplatePath = templateLocation,
                    OutputDirectory = TestUtils.CreateTemporaryFolder(),
                    DoNotAppendTemplateArgsToScenarioName = true,
                    DoNotPrependTemplateNameToScenarioName = true,
                    SnapshotsDirectory = "Approvals",
                    IsCommandExpectedToFail = true,
                    VerifyCommandOutput = true
                }
                .WithInstantiationThroughTemplateCreatorApi(new Dictionary<string, string?>());

            return new VerificationEngine(_log)
                .Execute(options);
        }
    }
}

#endif
