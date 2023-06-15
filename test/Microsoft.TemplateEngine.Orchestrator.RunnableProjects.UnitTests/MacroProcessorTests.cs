// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.UnitTests.Fakes;
using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.UnitTests
{
    public class MacroProcessorTests : IClassFixture<EnvironmentSettingsHelper>
    {
        private readonly EnvironmentSettingsHelper _environmentSettingsHelper;

        public MacroProcessorTests(EnvironmentSettingsHelper environmentSettingsHelper)
        {
            _environmentSettingsHelper = environmentSettingsHelper;
        }

        [Fact]
        public void CanThrow_WhenCannotProcessMacro()
        {
            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true, additionalComponents: new[] { (typeof(IMacro), (IIdentifiedComponent)new FailMacro()) });

            var macros = new[] { new FailMacroConfig("test") };

            MacroProcessingException e = Assert.Throws<MacroProcessingException>(() => MacroProcessor.ProcessMacros(engineEnvironmentSettings, macros, new VariableCollection()));
            Assert.Equal("Failed to evaluate", e.InnerException?.Message);
        }

        [Fact]
        public void CanPrintWarningOnUnknownMacroConfig()
        {
            List<(LogLevel Level, string Message)> loggedMessages = new();
            InMemoryLoggerProvider loggerProvider = new(loggedMessages);

            var engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(
                virtualize: true, environment: A.Fake<IEnvironment>(), addLoggerProviders: new[] { loggerProvider });

            var macroConfigs = new[] { new FakeMacroConfig(new FakeMacro(), "testVariable", "dummy") };
            MacroProcessor.ProcessMacros(engineEnvironmentSettings, macroConfigs, new VariableCollection());

            Assert.True(loggedMessages.Count == 1);
            Assert.Equal("Generated symbol 'testVariable': type 'fake' is unknown, processing is skipped.", loggedMessages.First().Message);
        }

        [Fact]
        public void CanProcessMacroWithCustomMacroAsDependency()
        {
            var fakeMacroVariableName = "testVariable";
            var coalesceVariableName = "coalesceTest";
            List<(LogLevel Level, string Message)> loggedMessages = new();
            InMemoryLoggerProvider loggerProvider = new(loggedMessages);
            var generatedConfig = A.Fake<IGeneratedSymbolConfig>();
            A.CallTo(() => generatedConfig.Parameters).Returns(new Dictionary<string, string>()
            {
                { "sourceVariableName",  JExtensions.ToJsonString("dummy") },
                { "fallbackVariableName",  JExtensions.ToJsonString(fakeMacroVariableName) }
            });
            A.CallTo(() => generatedConfig.VariableName).Returns(coalesceVariableName);

            var coalesceMacroConfig = new CoalesceMacroConfig(new CoalesceMacro(), generatedConfig);
            coalesceMacroConfig.Dependencies.Add(fakeMacroVariableName);

            var engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true, environment: A.Fake<IEnvironment>(), additionalComponents: new[] { (typeof(IMacro), (IIdentifiedComponent)new FakeMacro()) }, addLoggerProviders: new[] { loggerProvider });

            var fakeMacro = new FakeMacro();
            var customGeneratedConfig = A.Fake<IGeneratedSymbolConfig>();
            A.CallTo(() => customGeneratedConfig.Parameters).Returns(new Dictionary<string, string>()
            {
                { "source",  JExtensions.ToJsonString("dummy") },
                { "name",  JExtensions.ToJsonString(fakeMacroVariableName) },
            });
            A.CallTo(() => customGeneratedConfig.VariableName).Returns(fakeMacroVariableName);
            var fakeMacroConfig = new FakeMacroConfig(new FakeMacro(), customGeneratedConfig);
            var variableCollection = new Dictionary<string, object>() { { fakeMacroVariableName, fakeMacro } };

            MacroProcessor.ProcessMacros(engineEnvironmentSettings, new[] { (IMacroConfig)coalesceMacroConfig, fakeMacroConfig }, new VariableCollection(default, variableCollection));

            Assert.True(variableCollection.Count == 2);
            Assert.True(variableCollection.Values.Select(v => v.GetHashCode() == fakeMacro.GetHashCode()).Count() == 2);
            variableCollection.Select(v => v.Key).Should().Equal(new[] { fakeMacroVariableName, coalesceVariableName });
            Assert.True(coalesceMacroConfig.Dependencies.Count == 1);
            Assert.Equal(fakeMacroVariableName, coalesceMacroConfig.Dependencies.First());
        }

        [Fact]
        public void CanProcessCustomMacroWithDeps()
        {
            var dependentMacroVariableName = "testVariable";
            List<(LogLevel Level, string Message)> loggedMessages = new();
            InMemoryLoggerProvider loggerProvider = new(loggedMessages);

            var coalesceMacroName = "coalesceMacro";
            var coalesceGeneratedConfig = A.Fake<IGeneratedSymbolConfig>();
            A.CallTo(() => coalesceGeneratedConfig.Parameters).Returns(new Dictionary<string, string>()
            {
                { "sourceVariableName",  JExtensions.ToJsonString("dummy") },
                { "fallbackVariableName",  JExtensions.ToJsonString("dummy") }
            });
            A.CallTo(() => coalesceGeneratedConfig.VariableName).Returns(coalesceMacroName);
            var coalesceMacroConfig = new CoalesceMacroConfig(new CoalesceMacro(), coalesceGeneratedConfig);

            var switchMacroName = "switchMacro";
            var switchMacroConfig = new SwitchMacroConfig(new SwitchMacro(), switchMacroName, string.Empty, string.Empty, new List<(string?, string)>());

            var customMacroName = "customMacro";
            var customGeneratedConfig = A.Fake<IGeneratedSymbolConfig>();
            A.CallTo(() => customGeneratedConfig.Parameters).Returns(new Dictionary<string, string>()
            {
                { "source",  JExtensions.ToJsonString(dependentMacroVariableName) },
            });
            A.CallTo(() => customGeneratedConfig.VariableName).Returns(customMacroName);
            var customMacroConfig = new FakeMacroConfig(new FakeMacro(), customGeneratedConfig);
            customMacroConfig.Dependencies.Add(coalesceGeneratedConfig.VariableName);
            customMacroConfig.Dependencies.Add(switchMacroName);

            var engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(
                virtualize: true,
                environment: A.Fake<IEnvironment>(),
                addLoggerProviders: new[] { loggerProvider },
                additionalComponents: new[] { (typeof(IMacro), (IIdentifiedComponent)new FakeMacro()) });
            var variableCollection = new VariableCollection();

            MacroProcessor.ProcessMacros(engineEnvironmentSettings, new[] { (BaseMacroConfig)switchMacroConfig, customMacroConfig, coalesceMacroConfig }, variableCollection);

            // Custom macro was processed without errors
            Assert.True(!loggedMessages.Any(lm => lm.Level == LogLevel.Error));
        }

        [Fact]
        public void CanSortCollectionWithCustomMacroWithDeps()
        {
            var coalesceMacroName = "coalesceMacro";
            var coalesceGeneratedConfig = A.Fake<IGeneratedSymbolConfig>();
            A.CallTo(() => coalesceGeneratedConfig.Parameters).Returns(new Dictionary<string, string>()
            {
                { "sourceVariableName",  JExtensions.ToJsonString("dummy") },
                { "fallbackVariableName",  JExtensions.ToJsonString("dummy") }
            });
            A.CallTo(() => coalesceGeneratedConfig.VariableName).Returns(coalesceMacroName);
            var coalesceMacroConfig = new CoalesceMacroConfig(new CoalesceMacro(), coalesceGeneratedConfig);

            var switchMacroName = "switchMacro";
            var switchMacroConfig = new SwitchMacroConfig(new SwitchMacro(), switchMacroName, string.Empty, string.Empty, new List<(string?, string)>());

            var customMacroName = "customMacro";
            var customGeneratedConfig = A.Fake<IGeneratedSymbolConfig>();
            A.CallTo(() => customGeneratedConfig.Parameters).Returns(new Dictionary<string, string>()
            {
                { "source",  JExtensions.ToJsonString("dummy") },
            });
            A.CallTo(() => customGeneratedConfig.VariableName).Returns(customMacroName);
            var customMacroConfig = new FakeMacroConfig(new FakeMacro(), customGeneratedConfig);
            customMacroConfig.Dependencies.Add(coalesceGeneratedConfig.VariableName);
            customMacroConfig.Dependencies.Add(switchMacroName);

            var engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(
                virtualize: true,
                environment: A.Fake<IEnvironment>(),
                additionalComponents: new[] { (typeof(IMacro), (IIdentifiedComponent)new FakeMacro()) });
            var variableCollection = new VariableCollection();

            var sortedItems = MacroProcessor.SortMacroConfigsByDependencies(new[] { customMacroName, switchMacroName, coalesceMacroName }, new[] { (BaseMacroConfig)switchMacroConfig, customMacroConfig, coalesceMacroConfig });

            sortedItems.Select(si => si.VariableName).Should().Equal(new[] { switchMacroName, coalesceMacroName, customMacroName });
        }

        [Fact]
        public void CanSortMacrosWithDependencies()
        {
            var switchMacroName = "switchMacro";
            var coalesceMacroName = "coalesceMacro";
            var evaluateMacroName = "evaluateMacro";
            var joinMacroName = "joinMacro";

            var symbols = new string[] { switchMacroName, coalesceMacroName, evaluateMacroName, joinMacroName };

            var evaluateMacroConfig = new EvaluateMacroConfig(evaluateMacroName, string.Empty, "condition");
            var fakeCoalesceGeneratedSymbols = A.Fake<IGeneratedSymbolConfig>();
            A.CallTo(() => fakeCoalesceGeneratedSymbols.Parameters).Returns(new Dictionary<string, string>()
            {
                { "sourceVariableName",  JExtensions.ToJsonString("dummy") },
                { "fallbackVariableName",  JExtensions.ToJsonString("dummy") }
            });
            A.CallTo(() => fakeCoalesceGeneratedSymbols.VariableName).Returns(coalesceMacroName);
            var coalesceMacroConfig = new CoalesceMacroConfig(new CoalesceMacro(), fakeCoalesceGeneratedSymbols);
            coalesceMacroConfig.Dependencies.Add(evaluateMacroName);

            var switchMacroConfig = new SwitchMacroConfig(new SwitchMacro(), switchMacroName, string.Empty, string.Empty, new List<(string?, string)>());
            switchMacroConfig.Dependencies.Add(coalesceMacroName);
            switchMacroConfig.Dependencies.Add(evaluateMacroName);

            var fakeJoinGeneratedSymbols = A.Fake<IGeneratedSymbolConfig>();
            A.CallTo(() => fakeJoinGeneratedSymbols.VariableName).Returns(joinMacroName);
            A.CallTo(() => fakeJoinGeneratedSymbols.Parameters).Returns(new Dictionary<string, string>()
            {
                { "symbols",  /*lang=json,strict*/ "[ {\"value\":\"dummy\"  } ]" },
                { "fallbackVariableName",  JExtensions.ToJsonString("dummy") }
            });
            var joinMacroConfig = new JoinMacroConfig(new JoinMacro(), fakeJoinGeneratedSymbols);
            joinMacroConfig.Dependencies.Add(switchMacroName);
            var macroConfigs = new[] { (BaseMacroConfig)joinMacroConfig, switchMacroConfig, evaluateMacroConfig, coalesceMacroConfig };

            var sortedItems = MacroProcessor.SortMacroConfigsByDependencies(symbols, macroConfigs);

            sortedItems.Select(si => si.VariableName).Should().Equal(new[] { evaluateMacroName, coalesceMacroName, switchMacroName, joinMacroName });
        }

        [Fact]
        public void CanThrowErrorOnSortWhenMacrosHaveDepsCircle()
        {
            var switchMacroName = "switchMacro";
            var coalesceMacroName = "coalesceMacro";

            var symbols = new string[] { switchMacroName, coalesceMacroName };

            var fakeCoalesceGeneratedSymbols = A.Fake<IGeneratedSymbolConfig>();
            A.CallTo(() => fakeCoalesceGeneratedSymbols.Parameters).Returns(new Dictionary<string, string>()
            {
                { "sourceVariableName",  JExtensions.ToJsonString("dummy") },
                { "fallbackVariableName",  JExtensions.ToJsonString("dummy") }
            });
            A.CallTo(() => fakeCoalesceGeneratedSymbols.VariableName).Returns(coalesceMacroName);
            var coalesceMacroConfig = new CoalesceMacroConfig(new CoalesceMacro(), fakeCoalesceGeneratedSymbols);
            coalesceMacroConfig.Dependencies.Add(switchMacroName);

            var switchMacroConfig = new SwitchMacroConfig(new SwitchMacro(), switchMacroName, string.Empty, string.Empty, new List<(string?, string)>());
            switchMacroConfig.Dependencies.Add(coalesceMacroName);
            var macroConfigs = new[] { (BaseMacroConfig)switchMacroConfig, coalesceMacroConfig };

            Action sorting = () => { MacroProcessor.SortMacroConfigsByDependencies(symbols, macroConfigs); };
            sorting.Should().Throw<TemplateAuthoringException>()
                .Which.Message.Should().Contain("Parameter conditions contain cyclic dependency: [switchMacro, coalesceMacro, switchMacro] that is preventing deterministic evaluation.");
        }

        [Fact]
        public void CanRunDeterministically_ComputedMacros()
        {
            UndeterministicMacro macro = new UndeterministicMacro();

            IEnvironment environment = A.Fake<IEnvironment>();
            A.CallTo(() => environment.GetEnvironmentVariable("TEMPLATE_ENGINE_ENABLE_DETERMINISTIC_MODE")).Returns("true");

            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true, environment: environment, additionalComponents: new[] { (typeof(IMacro), (IIdentifiedComponent)macro) });

            var macros = new[] { (BaseMacroConfig)new UndeterministicMacroConfig(macro, "test"), new GuidMacroConfig("test-guid", "string", "Nn", "n") };

            IVariableCollection collection = new VariableCollection();

            MacroProcessor.ProcessMacros(engineEnvironmentSettings, macros, collection);
            Assert.Equal("deterministic", collection["test"]);
            Assert.Equal(new Guid("12345678-1234-1234-1234-1234567890AB").ToString("n"), collection["test-guid"]);

            A.CallTo(() => environment.GetEnvironmentVariable("TEMPLATE_ENGINE_ENABLE_DETERMINISTIC_MODE")).Returns("false");
            collection = new VariableCollection();

            MacroProcessor.ProcessMacros(engineEnvironmentSettings, macros, collection);
            Assert.Equal("undeterministic", collection["test"]);
            Assert.NotEqual(new Guid("12345678-1234-1234-1234-1234567890AB").ToString("n"), collection["test-guid"]);
        }

        private class FailMacro : IMacro<FailMacroConfig>, IGeneratedSymbolMacro
        {
            public string Type => "fail";

            public Guid Id { get; } = new Guid("{3DBC6AAB-5D13-40E9-9EC8-0467A7AA7335}");

            public IMacroConfig CreateConfig(IEngineEnvironmentSettings environmentSettings, IGeneratedSymbolConfig generatedSymbolConfig) => new FailMacroConfig(generatedSymbolConfig.VariableName);

            public void Evaluate(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, FailMacroConfig config) => throw new Exception("Failed to evaluate");

            public void Evaluate(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, IGeneratedSymbolConfig generatedSymbolConfig) => throw new Exception("Failed to evaluate");

            public void EvaluateConfig(IEngineEnvironmentSettings environmentSettings, IVariableCollection vars, IMacroConfig config) => throw new Exception("Failed to evaluate");
        }

        private class FailConfigMacro : IMacro<FailMacroConfig>, IGeneratedSymbolMacro
        {
            public string Type => "fail";

            public Guid Id { get; } = new Guid("{3DBC6AAB-5D13-40E9-9EC8-0467A7AA7335}");

            public IMacroConfig CreateConfig(IEngineEnvironmentSettings environmentSettings, IGeneratedSymbolConfig generatedSymbolConfig) => new FailMacroConfig(generatedSymbolConfig.VariableName);

            public void Evaluate(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, FailMacroConfig config) => throw new Exception("Failed to evaluate");

            public void Evaluate(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, IGeneratedSymbolConfig generatedSymbolConfig) => throw new TemplateAuthoringException("bad config");

            public void EvaluateConfig(IEngineEnvironmentSettings environmentSettings, IVariableCollection vars, IMacroConfig config) => throw new TemplateAuthoringException("bad config");
        }

        private class FailMacroConfig : BaseMacroConfig
        {
            public FailMacroConfig(string variableName) : base("fail", variableName, "string")
            {
            }
        }

        private class UndeterministicMacro : IDeterministicModeMacro, IDeterministicModeMacro<UndeterministicMacroConfig>, IGeneratedSymbolMacro, IGeneratedSymbolMacro<UndeterministicMacroConfig>
        {
            public string Type => "undeterministic";

            public Guid Id { get; } = new Guid("{3DBC6AAB-5D13-40E9-9EC8-0467A7AA7335}");

            public UndeterministicMacroConfig CreateConfig(IEngineEnvironmentSettings environmentSettings, IGeneratedSymbolConfig generatedSymbolConfig)
            {
                return new UndeterministicMacroConfig(this, generatedSymbolConfig.VariableName);
            }

            public void Evaluate(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, UndeterministicMacroConfig config)
            {
                variables[config.VariableName] = "undeterministic";
            }

            public void EvaluateConfig(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, IMacroConfig config)
            {
                variables[config.VariableName] = "undeterministic";
            }

            public void EvaluateDeterministically(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, UndeterministicMacroConfig config)
            {
                variables[config.VariableName] = "deterministic";
            }

            public void EvaluateConfigDeterministically(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, IMacroConfig config)
            {
                variables[config.VariableName] = "deterministic";
            }

            IMacroConfig IGeneratedSymbolMacro.CreateConfig(IEngineEnvironmentSettings environmentSettings, IGeneratedSymbolConfig generatedSymbolConfig)
            {
                return new UndeterministicMacroConfig(this, generatedSymbolConfig.VariableName);
            }
        }

        private class UndeterministicMacroConfig : BaseMacroConfig
        {
            private readonly UndeterministicMacro _macro;

            public UndeterministicMacroConfig(UndeterministicMacro macro, string variableName) : base("undeterministic", variableName, "string")
            {
                _macro = macro;
            }
        }
    }
}
