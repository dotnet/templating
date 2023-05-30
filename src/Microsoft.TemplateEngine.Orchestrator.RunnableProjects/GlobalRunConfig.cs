// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ConfigModel;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    internal class GlobalRunConfig
    {
        public IReadOnlyList<IOperationProvider> Operations { get; init; } = Array.Empty<IOperationProvider>();

        public IVariableConfig VariableSetup { get; init; } = VariableConfig.Default;

        public IReadOnlyList<IReplacementTokens> Replacements { get; init; } = Array.Empty<IReplacementTokens>();

        public IReadOnlyList<CustomOperationModel> CustomOperations { get; init; } = Array.Empty<CustomOperationModel>();

        public IReadOnlyList<string> SymbolNames { get; init; } = Array.Empty<string>();

        public IReadOnlyList<BaseMacroConfig> Macros { get; init; } = Array.Empty<BaseMacroConfig>();
    }
}
