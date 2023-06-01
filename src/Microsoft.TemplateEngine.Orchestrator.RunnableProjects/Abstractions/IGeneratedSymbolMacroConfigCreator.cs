// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions
{
    /// <summary>
    /// An interface for getting macro config for generated symbol macro.
    /// </summary>
    internal interface IGeneratedSymbolMacroConfigCreator<T>
        where T : IMacroConfig
    {
        T CreateConfig(IEngineEnvironmentSettings environmentSettings, IGeneratedSymbolConfig deferredConfig);
    }
}
