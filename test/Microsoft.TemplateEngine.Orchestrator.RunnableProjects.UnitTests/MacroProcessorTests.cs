// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FakeItEasy;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros;
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
        public void CanRunDeterministically_ComputedMacros()
        {
            UndeterministicMacro macro = new UndeterministicMacro();

            IEnvironment environment = A.Fake<IEnvironment>();
            A.CallTo(() => environment.GetEnvironmentVariable("TEMPLATE_ENGINE_ENABLE_DETERMINISTIC_MODE")).Returns("true");

            IEngineEnvironmentSettings engineEnvironmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true, environment: environment, additionalComponents: new[] { (typeof(IGeneratedSymbolMacro), (IIdentifiedComponent)macro) });

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

            public void Evaluate(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, FailMacroConfig config) => throw new Exception("Failed to evaluate");

            public void Evaluate(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, IGeneratedSymbolConfig generatedSymbolConfig) => throw new Exception("Failed to evaluate");

            public void EvaluateConfig(IEngineEnvironmentSettings environmentSettings, IVariableCollection vars, IMacroConfig config) => throw new Exception("Failed to evaluate");
        }

        private class FailConfigMacro : IMacro<FailMacroConfig>, IGeneratedSymbolMacro
        {
            public string Type => "fail";

            public Guid Id { get; } = new Guid("{3DBC6AAB-5D13-40E9-9EC8-0467A7AA7335}");

            public void Evaluate(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, FailMacroConfig config) => throw new Exception("Failed to evaluate");

            public void Evaluate(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, IGeneratedSymbolConfig generatedSymbolConfig) => throw new TemplateAuthoringException("bad config");

            public void EvaluateConfig(IEngineEnvironmentSettings environmentSettings, IVariableCollection vars, IMacroConfig config) => throw new TemplateAuthoringException("bad config");
        }

        private class FailMacroConfig : BaseMacroConfig
        {
            public FailMacroConfig(string variableName) : base("fail", variableName, "string")
            {
            }

            internal override void Evaluate(IEngineEnvironmentSettings environmentSettings, IVariableCollection vars) => throw new Exception("Failed to evaluate");

            internal override void EvaluateDeterministically(IEngineEnvironmentSettings environmentSettings, IVariableCollection vars) => Evaluate(environmentSettings, vars);
        }

        private class UndeterministicMacro : IDeterministicModeMacro<UndeterministicMacroConfig>, IGeneratedSymbolMacro, IDeterministicModeMacro<IGeneratedSymbolConfig>
        {
            public string Type => "undeterministic";

            public Guid Id { get; } = new Guid("{3DBC6AAB-5D13-40E9-9EC8-0467A7AA7335}");

            public void Evaluate(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, UndeterministicMacroConfig config)
            {
                variables[config.VariableName] = "undeterministic";
            }

            public void Evaluate(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, IGeneratedSymbolConfig generatedSymbolConfig)
            {
                variables[generatedSymbolConfig.VariableName] = "undeterministic";
            }

            public void EvaluateConfig(IEngineEnvironmentSettings environmentSettings, IVariableCollection vars, IMacroConfig config) => throw new NotImplementedException();

            public void EvaluateDeterministically(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, UndeterministicMacroConfig config)
            {
                variables[config.VariableName] = "deterministic";
            }

            public void EvaluateDeterministically(IEngineEnvironmentSettings environmentSettings, IVariableCollection variables, IGeneratedSymbolConfig config)
            {
                variables[config.VariableName] = "deterministic";
            }
        }

        private class UndeterministicMacroConfig : BaseMacroConfig
        {
            private readonly UndeterministicMacro _macro;

            public UndeterministicMacroConfig(UndeterministicMacro macro, string variableName) : base("undeterministic", variableName, "string")
            {
                _macro = macro;
            }

            internal override void Evaluate(IEngineEnvironmentSettings environmentSettings, IVariableCollection vars) => _macro.Evaluate(environmentSettings, vars, this);

            internal override void EvaluateDeterministically(IEngineEnvironmentSettings environmentSettings, IVariableCollection vars) => _macro.EvaluateDeterministically(environmentSettings, vars, this);
        }
    }
}
