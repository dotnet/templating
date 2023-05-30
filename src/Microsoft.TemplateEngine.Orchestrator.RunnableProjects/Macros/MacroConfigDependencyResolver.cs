// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Core.Expressions.Cpp2;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros
{
    internal class MacroConfigDependencyResolver
    {
        private readonly IReadOnlyList<BaseMacroConfig> _macroConfigs;
        private readonly IReadOnlyList<string> _symbols;

        internal MacroConfigDependencyResolver(IReadOnlyList<string> symbols, IReadOnlyList<BaseMacroConfig> macroConfigs)
        {
            _macroConfigs = macroConfigs;
            _symbols = symbols;
        }

        public void Resolve(BaseMacroConfig macroConfig)
        {
            switch (macroConfig)
            {
                case EvaluateMacroConfig em:
                    Resolve(em);
                    break;

                case SwitchMacroConfig sm:
                    Resolve(sm);
                    break;

                case JoinMacroConfig jm:
                    Resolve(jm);
                    break;

                case CoalesceMacroConfig cm:
                    Resolve(cm);
                    break;

                case RegexMacroConfig rm:
                    Resolve(rm);
                    break;

                case RegexMatchMacroConfig rmm:
                    Resolve(rmm);
                    break;

                case ProcessValueFormMacroConfig pvm:
                    Resolve(pvm);
                    break;

                default:
                    break;
            }
        }

        private void Resolve(EvaluateMacroConfig evaluateMacroConfig) => PopulateMacroConfigDependencies(evaluateMacroConfig.Condition, evaluateMacroConfig);

        private void Resolve(SwitchMacroConfig switchMacroConfig)
        {
            foreach (var (condition, _) in switchMacroConfig.Cases)
            {
                if (!string.IsNullOrEmpty(condition))
                {
                    PopulateMacroConfigDependencies(condition!, switchMacroConfig);
                }
            }
        }

        private void Resolve(JoinMacroConfig joinMacroConfig) =>
            joinMacroConfig.Symbols.ForEach(s => PopulateMacroConfigDependencies(s.Value, joinMacroConfig));

        private void Resolve(CoalesceMacroConfig cMacroConfig)
        {
            PopulateMacroConfigDependencies(cMacroConfig.SourceVariableName, cMacroConfig);
            PopulateMacroConfigDependencies(cMacroConfig.FallbackVariableName, cMacroConfig);
        }

        private void Resolve(RegexMacroConfig rMacroConfig) =>
            PopulateMacroConfigDependencies(rMacroConfig.Source, rMacroConfig);

        private void Resolve(RegexMatchMacroConfig rmMacroConfig) =>
            PopulateMacroConfigDependencies(rmMacroConfig.Source, rmMacroConfig);

        private void Resolve(ProcessValueFormMacroConfig pvMacroConfig) =>
           PopulateMacroConfigDependencies(pvMacroConfig.SourceVariable, pvMacroConfig);

        private void PopulateMacroConfigDependencies(string condition, BaseMacroConfig currentMacro)
        {
            var referencedVariablesKeys = new HashSet<string>();
            var expression = Cpp2StyleEvaluatorDefinition.GetEvaluableExpression(
                NullLogger<RunnableProjectGenerator>.Instance,
                condition,
                new VariableCollection(null, _symbols.ToDictionary(s => s, s => s as object)),
                out var evaluableExpressionError,
                referencedVariablesKeys);

            referencedVariablesKeys.ForEach(rv => PopulateMacroConfigDependency(rv, currentMacro));
        }

        private void PopulateMacroConfigDependency(string referencedValue, BaseMacroConfig currentMacro)
        {
            var macroConfig = _macroConfigs.FirstOrDefault(mc => mc.VariableName == referencedValue);
            if (macroConfig != null)
            {
                currentMacro.Dependencies.Add(macroConfig);
            }
        }
    }
}
