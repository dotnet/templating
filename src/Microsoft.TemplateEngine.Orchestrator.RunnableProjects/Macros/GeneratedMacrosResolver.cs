// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros
{
    internal class GeneratedMacrosResolver
    {
        private readonly Dictionary<string, Func<IGeneratedSymbolMacro, IGeneratedSymbolConfig, BaseMacroConfig?>> _macroConfigDictionary = new()
            {
                { "switch", (m, c) => m is SwitchMacro sm ? new SwitchMacroConfig(sm, c) : null },
                { "coalesce", (m, c) => m is CoalesceMacro cm ? new CoalesceMacroConfig(cm, c) : null },
                { "casing", (m, c) => m is CaseChangeMacro cm ? new CaseChangeMacroConfig(cm, c) : null },
                { "constant", (m, c) => m is ConstantMacro cm ? new ConstantMacroConfig(cm, c) : null },
                { "join", (m, c) => m is JoinMacro jm ? new JoinMacroConfig(jm, c) : null },
                { "regex", (m, c) => m is RegexMacro rm ? new RegexMacroConfig(rm, c) : null },
                { "regexMatch", (m, c) => m is RegexMatchMacro rmm ? new RegexMatchMacroConfig(rmm, c) : null }
            };

        public BaseMacroConfig? Resolve(IGeneratedSymbolMacro macro, IGeneratedSymbolConfig config) =>
            _macroConfigDictionary.TryGetValue(config.Type, out var factory) ? factory(macro, config) : null;
    }
}
