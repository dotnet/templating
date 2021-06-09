// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros.Config;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;
using static Microsoft.TemplateEngine.Orchestrator.RunnableProjects.RunnableProjectGenerator;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.UnitTests.MacroTests
{
    public class EvaluateMacroTests : IClassFixture<EnvironmentSettingsHelper>
    {
        private IEngineEnvironmentSettings _engineEnvironmentSettings;

        public EvaluateMacroTests(EnvironmentSettingsHelper environmentSettingsHelper)
        {
            _engineEnvironmentSettings = environmentSettingsHelper.CreateEnvironment(hostIdentifier: this.GetType().Name, virtualize: true);
        }

        [Theory(DisplayName = nameof(TestEvaluateConfig))]
        [InlineData("(7 > 3)", true)]
        [InlineData("(2 == 1)", false)]
        public void TestEvaluateConfig(string predicate, bool expectedResult)
        {
            string variableName = "myPredicate";
            string evaluator = "C++";
            EvaluateMacroConfig macroConfig = new EvaluateMacroConfig(variableName, null, predicate, evaluator);

            IVariableCollection variables = new VariableCollection();
            IRunnableProjectConfig config = new SimpleConfigModel(_engineEnvironmentSettings.Host.LoggerFactory);
            IParameterSet parameters = new ParameterSet(config);
            ParameterSetter setter = MacroTestHelpers.TestParameterSetter(_engineEnvironmentSettings, parameters);

            EvaluateMacro macro = new EvaluateMacro();
            macro.EvaluateConfig(_engineEnvironmentSettings, variables, macroConfig, parameters, setter);
            ITemplateParameter resultParam;
            Assert.True(parameters.TryGetParameterDefinition(variableName, out resultParam));
            bool resultValue = (bool)parameters.ResolvedValues[resultParam];
            Assert.Equal(resultValue, expectedResult);
        }
    }
}
