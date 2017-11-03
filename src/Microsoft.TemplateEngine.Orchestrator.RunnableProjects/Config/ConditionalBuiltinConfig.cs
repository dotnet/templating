using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Config
{
    internal static class ConditionalBuiltinConfig
    {
        public static List<IOperationProvider> GetConfigByType(ConditionalOperationConfigType configType)
        {
            JObject configObject = null;

            switch (configType)
            {
                case ConditionalOperationConfigType.CBlockComment:
                    configObject = CBlockCommentConfig;
                    break;
                case ConditionalOperationConfigType.CLineComment:
                    configObject = CLineCommentConfig;
                    break;
                case ConditionalOperationConfigType.CNoComment:
                    configObject = CNoCommentConfig;
                    break;
                case ConditionalOperationConfigType.HamlLineComment:
                    configObject = HamlLineCommentConfig;
                    break;
                case ConditionalOperationConfigType.HashLineComment:
                    configObject = HashLineCommentConfig;
                    break;
                case ConditionalOperationConfigType.JsxBlockComment:
                    configObject = JsxBlockCommentConfig;
                    break;
                case ConditionalOperationConfigType.MSBuildInline:
                    configObject = MSBuildInlineConfig;
                    break;
                case ConditionalOperationConfigType.None:
                    break;
                case ConditionalOperationConfigType.RazorBlockComment:
                    configObject = RazorBlockCommentConfig;
                    break;
                case ConditionalOperationConfigType.RemLineComment:
                    configObject = RemLineCommentConfig;
                    break;
                case ConditionalOperationConfigType.XmlBlockComment:
                    configObject = XmlBlockCommentConfig;
                    break;
                default:
                    throw new TemplateAuthoringException($"Template authoring error. Unhandled built in conditional configuration: {configType.ToString()}", "style");
            }

            if (configObject == null)
            {
                throw new TemplateAuthoringException($"Template authoring error. Unhandled built in conditional configuration: {configType.ToString()}", "style");
            }

            return ConditionalConfig.SetupFromJObject(configObject).ToList();
        }

        public static List<IOperationProvider> GetConfig(JObject rawConfig)
        {
            List<IOperationProvider> allOperations = new List<IOperationProvider>();
            IReadOnlyList<string> configNames = ConfigurationNamesFromConfig(rawConfig);

            if (configNames.Count == 0)
            {
                return allOperations;
            }

            foreach (string builtInConfigName in configNames)
            {
                if (!Enum.TryParse(builtInConfigName, out ConditionalOperationConfigType configType))
                {
                    throw new TemplateAuthoringException($"Template authoring error. Invalid built in conditional configuration: {builtInConfigName}", "style");
                }

                IEnumerable<IOperationProvider> configOperations = GetConfigByType(configType);
                allOperations.AddRange(configOperations);
            }

            return allOperations;
        }

        private static IReadOnlyList<string> ConfigurationNamesFromConfig(JObject rawConfig)
        {
            // "configuration" could be a single string value, or an array of strings.
            JToken builtInConfigsToken = rawConfig.Get<JToken>("configuration");
            if (builtInConfigsToken == null)
            {
                return new List<string>();
            }

            IReadOnlyList<string> configNames;
            if (builtInConfigsToken.Type == JTokenType.String)
            {
                configNames = new List<string>()
                {
                    builtInConfigsToken.ToString()
                };
            }
            else
            {
                configNames = builtInConfigsToken.ArrayAsStrings();
            }

            return configNames;
        }

        private static JObject CBlockCommentConfig
        {
            get
            {
                string config = @"
{
  ""Style"": ""block"",
  ""StartToken"": ""/*"",
  ""EndToken"": ""*/"",
  ""PseudoEndToken"": ""* /"",
  ""KeywordPrefix"": ""#"",
  ""Evaluator"": ""C++"",
  ""WholeLine"": true,
  ""TrimWhitespace"": true,
}";
                return JObject.Parse(config);
            }
        }

        private static JObject CLineCommentConfig
        {
            get
            {
                string config = @"
{
  ""Style"": ""line"",
  ""Token"": ""//"",
  ""KeywordPrefix"": ""#"",
  ""Evaluator"": ""C++"",
  ""WholeLine"": true,
  ""TrimWhitespace"": true,
  ""Id"": null,
}";
                return JObject.Parse(config);
            }
        }

    private static JObject CNoCommentConfig
        {
            get
            {
                string config = @"
{
  ""If"": [
    ""#if""
  ],
  ""Else"": [
    ""#else""
  ],
  ""ElseIf"": [
    ""#elseif"",
    ""#elif""
  ],
  ""EndIf"": [
    ""#endif""
  ],
  ""Trim"": true,
  ""WholeLine"": true,
  ""Evaluator"": ""C++""
}";
                return JObject.Parse(config);
            }
        }

        private static JObject HamlLineCommentConfig
        {
            get
            {
                string config = @"
{
  ""Style"": ""line"",
  ""Token"": ""-#"",
  ""KeywordPrefix"": ""#"",
  ""Evaluator"": ""C++"",
  ""WholeLine"": true,
  ""TrimWhitespace"": true,
  ""Id"": null,
}";
                return JObject.Parse(config);
            }
        }

        private static JObject HashLineCommentConfig
        {
            get
            {
                string config = @"
{
  ""Style"": ""line"",
  ""Token"": ""#"",
  ""KeywordPrefix"": """",
  ""Evaluator"": ""C++"",
  ""WholeLine"": true,
  ""TrimWhitespace"": true,
  ""Id"": null,
}";
                return JObject.Parse(config);
            }
        }

        private static JObject JsxBlockCommentConfig
        {
            get
            {
                string config = @"
{
  ""Style"": ""block"",
  ""StartToken"": ""{/*"",
  ""EndToken"": ""*/}"",
  ""PseudoEndToken"": ""* /}"",
  ""KeywordPrefix"": ""#"",
  ""Evaluator"": ""C++"",
  ""WholeLine"": true,
  ""TrimWhitespace"": true,
}";
                return JObject.Parse(config);
            }
        }

        private static JObject MSBuildInlineConfig
        {
            get
            {
                string config = @"
{
  ""Style"": ""msbuild"",
  ""OpenOpenToken"": ""<"",
  ""OpenCloseToken"": ""</"",
  ""CloseToken"": "">"",
  ""SelfClosingEndToken"": ""/>"",
  ""ConditionOpenExpression"": ""Condition=\"""",
  ""ConditionCloseExpression"": ""\"""",
  ""VariableFormat"": ""$({0})"",
  ""Id"": ""msbuild-conditional"",
  ""Trim"": true,
  ""WholeLine"": true,
  ""Evaluator"": ""MSBUILD""
}";
                return JObject.Parse(config);
            }
        }

        private static JObject RazorBlockCommentConfig
        {
            get
            {
                string config = @"
{
  ""Style"": ""block"",
  ""StartToken"": ""@*"",
  ""EndToken"": ""*@"",
  ""PseudoEndToken"": ""* @"",
  ""KeywordPrefix"": ""#"",
  ""Evaluator"": ""C++"",
  ""WholeLine"": true,
  ""TrimWhitespace"": true,
}";
                return JObject.Parse(config);
            }
        }

        private static JObject RemLineCommentConfig
        {
            get
            {
                string config = @"
{
  ""Style"": ""line"",
  ""Token"": ""rem "",
  ""KeywordPrefix"": ""#"",
  ""Evaluator"": ""C++"",
  ""WholeLine"": true,
  ""TrimWhitespace"": true,
  ""Id"": null,
}";
                return JObject.Parse(config);
            }
        }

        private static JObject XmlBlockCommentConfig
        {
            get
            {
                string config = @"
{
  ""Style"": ""block"",
  ""StartToken"": ""<!--"",
  ""EndToken"": ""-->"",
  ""PseudoEndToken"": ""-- >"",
  ""KeywordPrefix"": ""#"",
  ""Evaluator"": ""C++"",
  ""WholeLine"": true,
  ""TrimWhitespace"": true,
}";
                return JObject.Parse(config);
            }
        }
    }
}
