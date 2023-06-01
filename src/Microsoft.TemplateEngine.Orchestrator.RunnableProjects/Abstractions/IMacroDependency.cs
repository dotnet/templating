// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions
{
    internal interface IMacroDependency
    {
        void Resolve(
            IReadOnlyList<BaseMacroConfig> macroConfigs,
            IReadOnlyList<string> symbols,
            BaseMacroConfig macroConfig);
    }
}
