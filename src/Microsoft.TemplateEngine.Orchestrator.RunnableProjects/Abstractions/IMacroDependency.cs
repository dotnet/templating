// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions
{
    //TODO: doc
    public interface IMacroConfigDependency
    {
        public HashSet<string> Dependencies { get; }

        public void ResolveSymbolDependencies(IReadOnlyList<string> symbols);
    }
}
