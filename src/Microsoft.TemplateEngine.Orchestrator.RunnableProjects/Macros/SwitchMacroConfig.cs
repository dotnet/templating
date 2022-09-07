﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros
{
    internal class SwitchMacroConfig : BaseMacroConfig<SwitchMacro, SwitchMacroConfig>
    {
        private const EvaluatorType DefaultEvaluator = EvaluatorType.CPP2;

        internal SwitchMacroConfig(SwitchMacro macro, string variableName, string evaluator, string dataType, IReadOnlyList<(string?, string)> cases)
             : base(macro, variableName, dataType)
        {
            Evaluator = EvaluatorSelector.SelectStringEvaluator(EvaluatorSelector.ParseEvaluatorName(evaluator, DefaultEvaluator));
            Cases = cases;
        }

        internal SwitchMacroConfig(SwitchMacro macro, IGeneratedSymbolConfig generatedSymbolConfig)
        : base(macro, generatedSymbolConfig.VariableName, generatedSymbolConfig.DataType)
        {
            string? evaluator = GetOptionalParameterValue(generatedSymbolConfig, nameof(Evaluator));
            if (!string.IsNullOrWhiteSpace(evaluator))
            {
                Evaluator = EvaluatorSelector.SelectStringEvaluator(EvaluatorSelector.ParseEvaluatorName(evaluator, DefaultEvaluator));
            }
            List<(string? Condition, string Value)> cases = new();
            JArray jArray = GetMandatoryParameterArray(generatedSymbolConfig, nameof(Cases));

            foreach (JToken entry in jArray)
            {
                if (entry is not JObject jobj)
                {
                    throw new TemplateAuthoringException($"Generated symbol '{generatedSymbolConfig.VariableName}': array '{nameof(Cases)}' should contain JSON objects.", generatedSymbolConfig.VariableName);
                }
                string? condition = jobj.ToString("condition");
                string? value = jobj.ToString("value");
                if (value == null)
                {
                    throw new TemplateAuthoringException($"Generated symbol '{generatedSymbolConfig.VariableName}': array '{nameof(Cases)}' should contain JSON objects with property 'value'.", generatedSymbolConfig.VariableName);
                }
                cases.Add((condition, value));
            }
            Cases = cases;
        }

        internal ConditionStringEvaluator Evaluator { get; private set; } = EvaluatorSelector.SelectStringEvaluator(DefaultEvaluator);

        internal IReadOnlyList<(string? Condition, string Value)> Cases { get; private set; }
    }
}
