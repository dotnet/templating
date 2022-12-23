// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros.Config;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.UnitTests.MacroTests
{
    public class CoalesceMacroTests : IClassFixture<EnvironmentSettingsHelper>
    {
        private readonly IEngineEnvironmentSettings _engineEnvironmentSettings;
        private readonly EnvironmentSettingsHelper _environmentSettingsHelper;

        public CoalesceMacroTests(EnvironmentSettingsHelper environmentSettingsHelper)
        {
            _engineEnvironmentSettings = environmentSettingsHelper.CreateEnvironment(hostIdentifier: GetType().Name, virtualize: true);
            _environmentSettingsHelper = environmentSettingsHelper;
        }

        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData("", "", null, "")]
        [InlineData(null, "fallback", null, "fallback")]
        [InlineData("", "fallback", null, "fallback")]
        [InlineData("def", "fallback", "def", "fallback")]
        [InlineData("def", "fallback", "", "def")]
        public void CoalesceMacroTest(string? sourceValue, string? fallbackValue, string? defaultValue, string? expectedResult)
        {
            CoalesceMacro macro = new();
            CoalesceMacroConfig macroConfig = new("test", "string", "varA", defaultValue, "varB");

            VariableCollection variables = new();
            if (sourceValue != null)
            {
                variables["varA"] = sourceValue;
            }
            if (fallbackValue != null)
            {
                variables["varB"] = fallbackValue;
            }

            macro.EvaluateConfig(_engineEnvironmentSettings, variables, macroConfig);

            if (expectedResult == null)
            {
                Assert.False(variables.ContainsKey("test"));
            }
            else
            {
                Assert.Equal(expectedResult, variables["test"]);
            }
        }

        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData("", "", null, "")]
        [InlineData(null, "fallback", null, "fallback")]
        [InlineData("", "fallback", null, "fallback")]
        [InlineData("def", "fallback", "def", "fallback")]
        [InlineData("def", "fallback", "", "def")]
        public void GeneratedSymbolTest(string? sourceValue, string? fallbackValue, string? defaultValue, string? expectedResult)
        {
            CoalesceMacro macro = new();

            VariableCollection variables = new();
            if (sourceValue != null)
            {
                variables["varA"] = sourceValue;
            }
            if (fallbackValue != null)
            {
                variables["varB"] = fallbackValue;
            }

            macro.EvaluateConfig(_engineEnvironmentSettings, variables, new CoalesceMacroConfig("test", "string", "varA", defaultValue, "varB"));

            if (expectedResult == null)
            {
                Assert.False(variables.ContainsKey("test"));
            }
            else
            {
                Assert.Equal(expectedResult, variables["test"]);
            }
        }

        [Fact]
        public void GeneratedSymbolTest_DefaultValueLeadsToFallback()
        {
            List<(LogLevel Level, string Message)> loggedMessages = new();
            InMemoryLoggerProvider loggerProvider = new(loggedMessages);
            IEngineEnvironmentSettings environmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true, addLoggerProviders: new[] { loggerProvider });

            CoalesceMacro macro = new();

            VariableCollection variables = new()
            {
                ["varA"] = 0,
                ["varB"] = 10
            };

            macro.EvaluateConfig(environmentSettings, variables, new CoalesceMacroConfig("test", "string", "varA", "0", "varB"));
            Assert.Equal(10, variables["test"]);
            Assert.Equal("[CoalesceMacro]: 'test': source value '0' is not used, because it is equal to default value '0'.", loggedMessages.First().Message);
        }

        [Fact]
        public void GeneratedSymbolTest_ExplicitDefaultValuesArePreserved()
        {
            List<(LogLevel Level, string Message)> loggedMessages = new();
            InMemoryLoggerProvider loggerProvider = new(loggedMessages);
            IEngineEnvironmentSettings environmentSettings = _environmentSettingsHelper.CreateEnvironment(virtualize: true, addLoggerProviders: new[] { loggerProvider });

            CoalesceMacro macro = new();

            VariableCollection variables = new()
            {
                ["varA"] = 0,
                ["varB"] = 10
            };

            macro.EvaluateConfig(environmentSettings, variables, new CoalesceMacroConfig("test", "string", "varA", null, "varB"));
            Assert.Equal(0, variables["test"]);
        }
    }
}
