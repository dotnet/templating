using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Operations;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Config
{
    public class ReplacementConfig : IOperationConfig
    {
        public string Key => Replacement.OperationName;

        public Guid Id => new Guid("62DB7F1F-A10E-46F0-953F-A28A03A81CD1");

        public static IOperationProvider Setup(IEngineEnvironmentSettings environmentSettings, IReplacementTokens tokens, IParameterSet parameters)
        {
            if (parameters.TryGetRuntimeValue(environmentSettings, tokens.VariableName, out object newValueObject))
            {
                string newValue = newValueObject.ToString();
                return new Replacement(tokens.OriginalValue, newValue, null, true);
            }
            else
            {
                environmentSettings.Host.LogDiagnosticMessage($"Couldn't find a parameter called {tokens.VariableName}", "Initialization", "ReplacementConfig.Setup");
                return null;
            }
        }

        public IEnumerable<IOperationProvider> ConfigureFromJObject(JObject rawConfiguration, IDirectory templateRoot)
        {
            string original = rawConfiguration.ToString("original");
            string replacement = rawConfiguration.ToString("replacement");
            string id = rawConfiguration.ToString("id");
            bool onByDefault = rawConfiguration.ToBool("onByDefault");

            JArray onlyIf = rawConfiguration.Get<JArray>("onlyIf");
            TokenConfig coreConfig = original.TokenConfigBuilder();

            if (onlyIf != null)
            {
                foreach (JToken entry in onlyIf.Children())
                {
                    if (!(entry is JObject x))
                    {
                        continue;
                    }

                    string before = entry.ToString("before");
                    string after = entry.ToString("after");
                    TokenConfig entryConfig = coreConfig;

                    if (!string.IsNullOrEmpty(before))
                    {
                        entryConfig = entryConfig.OnlyIfBefore(before);
                    }

                    if (!string.IsNullOrEmpty(after))
                    {
                        entryConfig = entryConfig.OnlyIfAfter(after);
                    }

                    yield return new Replacement(entryConfig, replacement, id, onByDefault);
                }
            }
            else
            {
                yield return new Replacement(coreConfig, replacement, id, onByDefault);
            }
        }
    }
}
