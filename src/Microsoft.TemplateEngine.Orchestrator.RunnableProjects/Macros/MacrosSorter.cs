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
    internal class MacrosSorter
    {
        private readonly IReadOnlyList<string> _symbols;
        private readonly IReadOnlyList<BaseMacroConfig> _macroConfigs;

        internal MacrosSorter(IReadOnlyList<string> symbols, IReadOnlyList<BaseMacroConfig> macroConfigs)
        {
            _symbols = symbols;
            _macroConfigs = macroConfigs;
            CollectMacroConfigDependencies();
        }

        public void SortMacroConfigsByDependencies(out IReadOnlyList<BaseMacroConfig> macroConfigs)
        {
            DirectedGraph<BaseMacroConfig> parametersDependenciesGraph = new(_macroConfigs.ToDictionary(mc => mc, mc => mc.Dependencies));
            if (!parametersDependenciesGraph.TryGetTopologicalSort(out macroConfigs) && parametersDependenciesGraph.HasCycle(out var cycle))
            {
                throw new TemplateAuthoringException(
                    string.Format(
                        LocalizableStrings.Authoring_CyclicDependencyInSymbols,
                        cycle.Select(p => p.VariableName).ToCsvString()),
                    "Symbol circle");
            }
        }

        private void CollectMacroConfigDependencies()
        {
            foreach (var macroConfig in _macroConfigs)
            {
                if (macroConfig is EvaluateMacroConfig evaluateMacroConfig)
                {
                    PopulateMacroConfigEvaluableDependencies(evaluateMacroConfig.Condition, evaluateMacroConfig);
                }

                if (macroConfig is SwitchMacroConfig switchMacroConfig)
                {
                    foreach (var (condition, _) in switchMacroConfig.Cases)
                    {
                        if (!string.IsNullOrEmpty(condition))
                        {
                            PopulateMacroConfigEvaluableDependencies(condition!, switchMacroConfig);
                        }
                    }
                }

                if (macroConfig is JoinMacroConfig joinMacroConfig)
                {
                    joinMacroConfig.Symbols.ForEach(s => PopulateMacroConfigDependency(s.Value, joinMacroConfig));
                }

                if (macroConfig is CoalesceMacroConfig cMacroConfig)
                {
                    PopulateMacroConfigDependency(cMacroConfig.SourceVariableName, cMacroConfig);
                }
            }
        }

        private void PopulateMacroConfigEvaluableDependencies(string condition, BaseMacroConfig currentMacro)
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
