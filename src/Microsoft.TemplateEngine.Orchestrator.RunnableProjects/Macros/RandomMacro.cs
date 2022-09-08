// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros.Config;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros
{
    internal class RandomMacro : IMacro, IDeferredMacro
    {
        public Guid Id => new Guid("011E8DC1-8544-4360-9B40-65FD916049B7");

        public string Type => "random";

        public void EvaluateConfig(IEngineEnvironmentSettings environmentSettings, IVariableCollection vars, IMacroConfig rawConfig)
        {
            RandomMacroConfig? config = rawConfig as RandomMacroConfig;

            if (config == null)
            {
                throw new InvalidCastException("Couldn't cast the rawConfig as RandomMacroConfig");
            }

            int value = CryptoRandom.NextInt(config.Low, config.High);
            vars[config.VariableName] = value;
        }

        public IMacroConfig CreateConfig(IEngineEnvironmentSettings environmentSettings, IMacroConfig rawConfig)
        {
            GeneratedSymbolDeferredMacroConfig? deferredConfig = rawConfig as GeneratedSymbolDeferredMacroConfig;

            if (deferredConfig == null)
            {
                throw new InvalidCastException("Couldn't cast the rawConfig as a GeneratedSymbolDeferredMacroConfig");
            }

            int low;
            int high;

            if (!deferredConfig.Parameters.TryGetValue("low", out JToken lowToken))
            {
                throw new ArgumentNullException("low");
            }
            else
            {
                low = lowToken.Value<int>();
            }

            if (!deferredConfig.Parameters.TryGetValue("high", out JToken highToken))
            {
                high = int.MaxValue;
            }
            else
            {
                high = highToken.Value<int>();
            }

            IMacroConfig realConfig = new RandomMacroConfig(deferredConfig.VariableName, deferredConfig.DataType, low, high);
            return realConfig;
        }
    }
}
