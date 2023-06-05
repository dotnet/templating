// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions
{
    /// <summary>
    /// An interface for macro created via generated symbols.
    /// </summary>
    public interface IGeneratedSymbolMacro : IMacro
    {
        IMacroConfig CreateConfig(IEngineEnvironmentSettings environmentSettings, IGeneratedSymbolConfig generatedSymbolConfig);
    }

    /// <summary>
    /// An interface for macro created via generated symbols.
    /// </summary>
    public interface IGeneratedSymbolMacro<T> : IMacro<T> where T : IMacroConfig
    {
        T CreateConfig(IEngineEnvironmentSettings environmentSettings, IGeneratedSymbolConfig generatedSymbolConfig);
    }
}
