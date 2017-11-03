using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Operations;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Config
{
    public class ConditionalConfig : IOperationConfig
    {
        public string Key => Conditional.OperationName;

        public Guid Id => new Guid("3E8BCBF0-D631-45BA-A12D-FBF1DE03AA38");

        public IEnumerable<IOperationProvider> ConfigureFromJObject(JObject rawConfiguration, IDirectory templateRoot)
        {
            IEnumerable<IOperationProvider> operations = SetupFromJObject(rawConfiguration);

            foreach (IOperationProvider op in operations)
            {
                yield return op;
            }
        }

        public static IEnumerable<IOperationProvider> SetupFromJObject(JObject rawConfiguration)
        {
            string commentStyle = rawConfiguration.ToString("style");
            IEnumerable<IOperationProvider> operations = null;

            if (string.IsNullOrEmpty(commentStyle) || string.Equals(commentStyle, "custom", StringComparison.OrdinalIgnoreCase))
            {
                operations = ConditionalCustomConfig.ConfigureFromJObject(rawConfiguration);
            }
            else if (string.Equals(commentStyle, "line", StringComparison.OrdinalIgnoreCase))
            {
                operations = ConditionalLineCommentConfig.ConfigureFromJObject(rawConfiguration);
            }
            else if (string.Equals(commentStyle, "block", StringComparison.OrdinalIgnoreCase))
            {
                operations = ConditionalBlockCommentConfig.ConfigureFromJObject(rawConfiguration);
            }
            else if (string.Equals(commentStyle, "builtin", StringComparison.OrdinalIgnoreCase))
            {
                operations = ConditionalBuiltinConfig.GetConfig(rawConfiguration);
            }
            else if (string.Equals(commentStyle, "msbuild", StringComparison.OrdinalIgnoreCase))
            {
                operations = MSBuildConditionalSetupFromJObject(rawConfiguration);
            }
            else
            {
                throw new TemplateAuthoringException($"Template authoring error. Invalid comment style [{commentStyle}].", "style");
            }

            return operations;
        }

        public static List<IOperationProvider> MSBuildConditionalSetupFromJObject(JObject rawConfig)
        {
            string openOpenToken = rawConfig.ToString("OpenOpenToken");
            string openCloseToken = rawConfig.ToString("OpenCloseToken");
            string closeToken = rawConfig.ToString("CloseToken");
            string selfClosingEndToken = rawConfig.ToString("SelfClosingEndToken");
            string conditionOpenExpression = rawConfig.ToString("ConditionOpenExpression");
            string conditionCloseExpression = rawConfig.ToString("ConditionCloseExpression");
            string variableFormat = rawConfig.ToString("VariableFormat");
            string id = rawConfig.ToString("Id");
            bool trim = rawConfig.ToBool("Trim", true);
            bool wholeline = rawConfig.ToBool("WholeLine", true);
            bool onByDefault = rawConfig.ToBool("OnByDefault", true);
            string evaluatorType = rawConfig.ToString("evaluator");

            ConditionEvaluator evaluator = EvaluatorSelector.Select(evaluatorType);
            MarkupTokens tokens = new MarkupTokens(openOpenToken.TokenConfig(), openCloseToken.TokenConfig(), closeToken.TokenConfig(), selfClosingEndToken.TokenConfig(), conditionOpenExpression.TokenConfig(), conditionCloseExpression.TokenConfig());
            IOperationProvider conditional = new InlineMarkupConditional(tokens, wholeline, trim, evaluator, variableFormat, id, onByDefault);

            return new List<IOperationProvider>()
            {
                conditional
            };
        }
    }
}
