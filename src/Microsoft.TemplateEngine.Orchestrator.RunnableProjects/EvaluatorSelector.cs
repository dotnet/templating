﻿using System;
using Microsoft.TemplateEngine.Core.Expressions.Cpp;
using Microsoft.TemplateEngine.Core.Expressions.Cpp2;
using Microsoft.TemplateEngine.Core.Expressions.MSBuild;
using Microsoft.TemplateEngine.Core.Expressions.VisualBasic;
using Microsoft.TemplateEngine.Core.Operations;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    public static class EvaluatorSelector
    {
        public static ConditionEvaluator Select(string name, ConditionEvaluator @default = null)
        {
            @default = @default ?? CppStyleEvaluatorDefinition.Evaluate;
            string evaluatorName = name ?? string.Empty;
            ConditionEvaluator evaluator;

            switch (evaluatorName)
            {
                case "":
                    evaluator = @default;
                    break;
                case "C++2":
                    evaluator = Cpp2StyleEvaluatorDefinition.Evaluate;
                    break;
                case "C++":
                    evaluator = CppStyleEvaluatorDefinition.Evaluate;
                    break;
                case "MSBUILD":
                    evaluator = MSBuildStyleEvaluatorDefinition.Evaluate;
                    break;
                case "VB":
                    evaluator = VisualBasicStyleEvaluatorDefintion.Evaluate;
                    break;
                default:
                    throw new Exception($"Unrecognized evaluator {evaluatorName}");
            }

            return evaluator;
        }
    }
}
