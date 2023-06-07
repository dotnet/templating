// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions
{
    /// <summary>
    /// Represents a dependency configuration for a macro.
    /// </summary>
    public interface IMacroConfigDependency
    {
        /// <summary>
        /// Gets the set of dependencies required by the macro.
        /// </summary>
        HashSet<string> Dependencies { get; }

        /// <summary>
        /// Resolves the symbol dependencies based on the provided list of symbols.
        /// </summary>
        /// <param name="symbols">The list of symbols to resolve dependencies for.</param>
        void ResolveSymbolDependencies(IReadOnlyList<string> symbols);
    }

}
