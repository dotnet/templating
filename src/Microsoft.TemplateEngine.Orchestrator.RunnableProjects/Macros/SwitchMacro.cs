// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Expressions.Cpp2;
using Microsoft.TemplateEngine.Core.Operations;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros.Config;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros
{
    internal class SwitchMacro : IMacro, IDeferredMacro
    {
        internal const string DefaultEvaluator = "C++";

        public Guid Id => new Guid("B57D64E0-9B4F-4ABE-9366-711170FD5294");

        public string Type => "switch";

        public void EvaluateConfig(IEngineEnvironmentSettings environmentSettings, IVariableCollection vars, IMacroConfig rawConfig)
        {
            SwitchMacroConfig? config = rawConfig as SwitchMacroConfig;

            if (config == null)
            {
                throw new InvalidCastException("Couldn't cast the rawConfig as SwitchMacroConfig");
            }

            ConditionEvaluator evaluator = EvaluatorSelector.Select(config.Evaluator, Cpp2StyleEvaluatorDefinition.Evaluate);
            string result = string.Empty;   // default if no condition assigns a value

            foreach (KeyValuePair<string?, string?> switchInfo in config.Switches)
            {
                string? condition = switchInfo.Key;
                string? value = switchInfo.Value;

                if (string.IsNullOrEmpty(condition))
                {
                    // no condition, this is the default.
                    result = value ?? string.Empty;
                    break;
                }
                else
                {
                    byte[] conditionBytes = Encoding.UTF8.GetBytes(condition);
                    int length = conditionBytes.Length;
                    int position = 0;
                    IProcessorState state = new GlobalRunSpec.ProcessorState(environmentSettings, vars, conditionBytes, Encoding.UTF8);

                    if (evaluator(state, ref length, ref position, out bool faulted))
                    {
                        result = value ?? string.Empty;
                        break;
                    }
                }
            }
            vars[config.VariableName] = result.ToString();
        }

        public IMacroConfig CreateConfig(IEngineEnvironmentSettings environmentSettings, IMacroConfig rawConfig)
        {
            GeneratedSymbolDeferredMacroConfig? deferredConfig = rawConfig as GeneratedSymbolDeferredMacroConfig;

            if (deferredConfig == null)
            {
                throw new InvalidCastException("Couldn't cast the rawConfig as a SwitchMacroConfig");
            }

            string? evaluator = null;
            if (deferredConfig.Parameters.TryGetValue("evaluator", out JToken evaluatorToken))
            {
                evaluator = evaluatorToken.ToString();
            }

            string? dataType = null;
            if (deferredConfig.Parameters.TryGetValue("datatype", out JToken dataTypeToken))
            {
                dataType = dataTypeToken.ToString();
            }

            List<KeyValuePair<string?, string?>> switchList = new();
            if (deferredConfig.Parameters.TryGetValue("cases", out JToken switchListToken))
            {
                JArray switchJArray = (JArray)switchListToken;
                foreach (JToken switchInfo in switchJArray)
                {
                    JObject map = (JObject)switchInfo;
                    string? condition = map.ToString("condition");
                    string? value = map.ToString("value");
                    switchList.Add(new KeyValuePair<string?, string?>(condition, value));
                }
            }

            IMacroConfig realConfig = new SwitchMacroConfig(deferredConfig.VariableName, evaluator, dataType, switchList);
            return realConfig;
        }
    }
}
