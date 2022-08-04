// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros.Config;
using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateEngine.Utils;
using Xunit;

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
            EvaluateMacroConfig macroConfig = new EvaluateMacroConfig(variableName, "bool", predicate, evaluator);

            IVariableCollection variables = new VariableCollection();

            EvaluateMacro macro = new EvaluateMacro();
            macro.EvaluateConfig(_engineEnvironmentSettings, variables, macroConfig);
            Assert.Equal(variables[variableName], expectedResult);
        }

        [Theory(DisplayName = nameof(TestEvaluateConfig))]
        [InlineData("(Param == A)", "C++", "A|B", true)]
        [InlineData("(Param == C)", "C++", "A|B", false)]
        [InlineData("((Param == A) || (Param == B))", "C++", "A|B", true)]
        [InlineData("((Param == A) && (Param == B))", "C++", "A|B", true)]
        [InlineData("((Param == A) && (Param != B))", "C++", "A|B", false)]
        [InlineData("((Param == A) && (Param == C))", "C++", "A|B", false)]
        [InlineData("(Param == A)", "C++2", "A|B", true)]
        [InlineData("(Param == C)", "C++2", "A|B", false)]
        [InlineData("((Param == A) || (Param == B))", "C++2", "A|B", true)]
        [InlineData("((Param == A) && (Param == B))", "C++2", "A|B", true)]
        [InlineData("((Param == A) && (Param != B))", "C++2", "A|B", false)]
        [InlineData("((Param == A) && (Param == C))", "C++2", "A|B", false)]
        public void TestEvaluateMultichoice(string condition, string evaluator, string multichoiceValues, bool expectedResult)
        {
            string variableName = "myPredicate";
            EvaluateMacroConfig macroConfig = new EvaluateMacroConfig(variableName, null, condition, evaluator);

            IVariableCollection variables = new VariableCollection();
            variables["A"] = "A";
            variables["B"] = "B";
            variables["C"] = "C";
            variables["Param"] = new MultiValueParameter(multichoiceValues.TokenizeMultiValueParameter());

            EvaluateMacro macro = new EvaluateMacro();
            macro.EvaluateConfig(_engineEnvironmentSettings, variables, macroConfig);
            Assert.Equal(variables[variableName], expectedResult);
        }
    }
}
