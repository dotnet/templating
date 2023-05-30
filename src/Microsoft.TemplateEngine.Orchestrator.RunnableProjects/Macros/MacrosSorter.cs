// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros
{
    internal class MacrosSorter
    {
        private readonly IReadOnlyList<BaseMacroConfig> _macroConfigs;

        private readonly MacroConfigDependencyResolver _macroConfigsDependenciesResolver;

        internal MacrosSorter(IReadOnlyList<string> symbols, IReadOnlyList<BaseMacroConfig> macroConfigs)
        {
            _macroConfigs = macroConfigs;

            _macroConfigsDependenciesResolver = new MacroConfigDependencyResolver(symbols, macroConfigs);
            _macroConfigs.ForEach(_macroConfigsDependenciesResolver.Resolve);
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
    }
}
