// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros
{
    internal class CoalesceMacroConfig : BaseMacroConfig<CoalesceMacro, CoalesceMacroConfig>
    {
        internal CoalesceMacroConfig(
            CoalesceMacro macro,
            string variableName,
            string dataType,
            string sourceVariableName,
            string? defaultValue,
            string fallbackVariableName)
             : base(macro, variableName, dataType)
        {
            if (string.IsNullOrWhiteSpace(sourceVariableName))
            {
                throw new System.ArgumentException($"'{nameof(sourceVariableName)}' cannot be null or whitespace.", nameof(sourceVariableName));
            }

            if (string.IsNullOrWhiteSpace(fallbackVariableName))
            {
                throw new System.ArgumentException($"'{nameof(fallbackVariableName)}' cannot be null or whitespace.", nameof(fallbackVariableName));
            }

            SourceVariableName = sourceVariableName;
            DefaultValue = defaultValue;
            FallbackVariableName = fallbackVariableName;
        }

        internal CoalesceMacroConfig(CoalesceMacro macro, IGeneratedSymbolConfig generatedSymbolConfig)
            : base(macro, generatedSymbolConfig.VariableName, generatedSymbolConfig.DataType)
        {
            SourceVariableName = GetMandatoryParameterValue(generatedSymbolConfig, "sourceVariableName");
            FallbackVariableName = GetMandatoryParameterValue(generatedSymbolConfig, "fallbackVariableName");
            DefaultValue = GetOptionalParameterValue(generatedSymbolConfig, "defaultValue");
        }

        internal string SourceVariableName { get; }

        internal string? DefaultValue { get; }

        internal string FallbackVariableName { get; }
    }
}
