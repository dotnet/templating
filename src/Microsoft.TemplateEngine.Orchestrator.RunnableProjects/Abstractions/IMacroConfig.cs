// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions
{
    /// <summary>
    /// Base interface for macro configuration.
    /// </summary>
    public interface IMacroConfig
    {
        /// <summary>
        /// The variable name that should be used for storing the result of evaluation.
        /// </summary>
        string VariableName { get; }

        /// <summary>
        /// Gets the type of <see cref="IMacro"/>.
        /// </summary>
        string Type { get; }
    }
}
