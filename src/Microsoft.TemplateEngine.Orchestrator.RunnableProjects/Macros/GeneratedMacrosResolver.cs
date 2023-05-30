// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros
{
    internal class GeneratedMacrosResolver
    {
        private readonly Dictionary<string, Func<IGeneratedSymbolMacro, IGeneratedSymbolConfig, IEngineEnvironmentSettings, BaseMacroConfig?>> _macroConfigDictionary = new()
            {
                { "switch", (m, c, _) => m is SwitchMacro sm ? new SwitchMacroConfig(sm, c) : null },
                { "coalesce", (m, c, _) => m is CoalesceMacro cm ? new CoalesceMacroConfig(cm, c) : null },
                { "casing", (m, c, _) => m is CaseChangeMacro cm ? new CaseChangeMacroConfig(cm, c) : null },
                { "constant", (m, c, _) => m is ConstantMacro cm ? new ConstantMacroConfig(cm, c) : null },
                { "join", (m, c, _) => m is JoinMacro jm ? new JoinMacroConfig(jm, c) : null },
                { "regex", (m, c, _) => m is RegexMacro rm ? new RegexMacroConfig(rm, c) : null },
                { "regexMatch", (m, c, _) => m is RegexMatchMacro rmm ? new RegexMatchMacroConfig(rmm, c) : null },
                { "port", (m, c, settings) => m is GeneratePortNumberMacro gpn ? new GeneratePortNumberConfig(settings.Host.Logger, gpn, c) : null },
                { "random", (m, c, settings) => m is RandomMacro rm ? new RandomMacroConfig(rm, c) : null },
                { "guid", (m, c, settings) => m is GuidMacro gm ? new GuidMacroConfig(gm, c) : null },
                { "now", (m, c, settings) => m is NowMacro nm ? new NowMacroConfig(nm, c) : null }
            };

        public BaseMacroConfig? Resolve(IGeneratedSymbolMacro macro, IGeneratedSymbolConfig config, IEngineEnvironmentSettings settings) =>
            _macroConfigDictionary.TryGetValue(config.Type, out var factory) ? factory(macro, config, settings) : null;
    }
}
