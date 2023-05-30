// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
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
                // Errors in macro dependencies are not supposed to interrupt template generation.
                // Skip this macro and add info in the output.
                if (config.Dependencies.Any() && config.Dependencies.Any(d => d.MacroErrors.Any()))
                {
                    environmentSettings.Host.Logger.LogWarning(
                        string.Format(
                            LocalizableStrings.MacroProcessing_Warning_DependencyErrors,
                            config.VariableName,
                            string.Join(",", config.Dependencies.SelectMany(d => d.MacroErrors))));

                    continue;
                }

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
                //TemplateAuthoringException means that config was invalid, just pass it.
                catch (Exception ex) when (ex is not TemplateAuthoringException)
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
