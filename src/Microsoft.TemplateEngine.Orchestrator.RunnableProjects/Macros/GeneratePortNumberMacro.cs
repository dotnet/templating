// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros.Config;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros
{
    internal class GeneratePortNumberMacro : IDeferredMacro
    {
        private const int LowPortDefault = 1024;
        private const int HighPortDefault = 65535;

        public Guid Id => new Guid("D49B3690-B1E5-410F-A260-E1D7E873D8B2");

        public string Type => "port";

        public void EvaluateConfig(IEngineEnvironmentSettings environmentSettings, IVariableCollection vars, IMacroConfig rawConfig)
        {
            GeneratePortNumberConfig config = rawConfig as GeneratePortNumberConfig;

            if (config == null)
            {
                throw new InvalidCastException("Couldn't cast the rawConfig as EvaluateMacroConfig");
            }

            config.Socket?.Dispose();
            vars[config.VariableName] = config.Port.ToString();
        }

        public IMacroConfig CreateConfig(IEngineEnvironmentSettings environmentSettings, IMacroConfig rawConfig)
        {
            GeneratedSymbolDeferredMacroConfig deferredConfig = rawConfig as GeneratedSymbolDeferredMacroConfig;

            if (deferredConfig == null)
            {
                throw new InvalidCastException("Couldn't cast the rawConfig as a GeneratedSymbolDeferredMacroConfig");
            }

            int low;
            int high;

            if (!deferredConfig.Parameters.TryGetValue("low", out string lowToken))
            {
                low = LowPortDefault;
            }
            else
            {
                JToken token = JToken.Parse(lowToken);
                if (token.Type == JTokenType.Integer)
                {
                    low = token.Value<int>();
                    if (low < LowPortDefault)
                    {
                        low = LowPortDefault;
                    }
                }
                else
                {
                    low = LowPortDefault;
                }
            }

            if (!deferredConfig.Parameters.TryGetValue("high", out string highToken))
            {
                high = HighPortDefault;
            }
            else
            {
                JToken token = JToken.Parse(highToken);
                if (token.Type == JTokenType.Integer)
                {
                    high = token.Value<int>();
                    if (high > HighPortDefault)
                    {
                        high = HighPortDefault;
                    }
                }
                else
                {
                    high = HighPortDefault;
                }
            }

            if (low > high)
            {
                low = LowPortDefault;
                high = HighPortDefault;
            }

            int fallback = 0;
            if (deferredConfig.Parameters.TryGetValue("fallback", out string fallbackToken))
            {
                JToken token = JToken.Parse(fallbackToken);
                if (token.Type == JTokenType.Integer)
                {
                    fallback = token.ToInt32();
                }
            }

            IMacroConfig realConfig = new GeneratePortNumberConfig(deferredConfig.VariableName, deferredConfig.DataType, fallback, low, high);
            return realConfig;
        }
    }
}
