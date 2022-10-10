// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros
{
    internal class CoalesceMacro : BaseGeneratedSymbolMacro<CoalesceMacroConfig>
    {
        public override string Type => "coalesce";

        public override Guid Id { get; } = new("11C6EACF-8D24-42FD-8FC6-84063FCD8F14");

        public override void Evaluate(IEngineEnvironmentSettings environmentSettings, IVariableCollection variableCollection, CoalesceMacroConfig config)
        {
            if (variableCollection.TryGetValue(config.SourceVariableName, out object currentSourceValue) && currentSourceValue != null)
            {
                if (config.DefaultValue != null && currentSourceValue.Equals(config.DefaultValue))
                {
                    environmentSettings.Host.Logger.LogDebug("[{macro}]: '{var}': source value '{source}' is not used, because it is equal to default value '{default}'.", nameof(CoalesceMacro), config.VariableName, currentSourceValue, config.DefaultValue);
                }
                else
                {
                    variableCollection[config.VariableName] = currentSourceValue;
                    environmentSettings.Host.Logger.LogDebug("[{macro}]: Assigned variable '{var}' to '{value}'.", nameof(CoalesceMacro), config.VariableName, currentSourceValue);
                    return;
                }
            }
            if (variableCollection.TryGetValue(config.FallbackVariableName, out object currentFallbackValue) && currentFallbackValue != null)
            {
                variableCollection[config.VariableName] = currentFallbackValue;
                environmentSettings.Host.Logger.LogDebug("[{macro}]: Assigned variable '{var}' to fallback value '{value}'.", nameof(CoalesceMacro), config.VariableName, currentFallbackValue);
                return;
            }
            environmentSettings.Host.Logger.LogDebug("[{macro}]: Variable '{var}' was not assigned, neither source nor fallback variable was found.", nameof(CoalesceMacro), config.VariableName);
        }

        protected override CoalesceMacroConfig CreateConfig(IGeneratedSymbolConfig deferredConfig) => new(this, deferredConfig);
    }
}
