// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    internal static class MacroProcessor
    {
        /// <summary>
        /// Processes the macros defined in <paramref name="macroConfigs"/>.
        /// </summary>
        /// <exception cref="TemplateAuthoringException">when <see cref="IGeneratedSymbolMacro"/> config is invalid.</exception>
        /// <exception cref="MacroProcessingException">when the error occurs when macro is processed.</exception>
        internal static void ProcessMacros(
            IEngineEnvironmentSettings environmentSettings,
            IReadOnlyList<BaseMacroConfig> macroConfigs,
            IVariableCollection variables)
        {
            bool deterministicMode = IsDeterministicModeEnabled(environmentSettings);

            foreach (BaseMacroConfig config in macroConfigs)
            {
                try
                {
                    if (deterministicMode)
                    {
                        config.EvaluateDeterministically(environmentSettings, variables);
                    }
                    else
                    {
                        config.Evaluate(environmentSettings, variables);
                    }
                }
                catch (Exception ex)
                {
                    throw new MacroProcessingException(config, ex);
                }
            }
        }

        private static bool IsDeterministicModeEnabled(IEngineEnvironmentSettings environmentSettings)
        {
            string? unparsedValue = environmentSettings.Environment.GetEnvironmentVariable("TEMPLATE_ENGINE_ENABLE_DETERMINISTIC_MODE");
            return bool.TryParse(unparsedValue, out bool result) && result;
        }
    }
}
