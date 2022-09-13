// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Operations;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.OperationConfig
{
    internal class ReplacementConfig : IOperationConfig
    {
        public string Key => Replacement.OperationName;

        public Guid Id => new Guid("62DB7F1F-A10E-46F0-953F-A28A03A81CD1");

        public IEnumerable<IOperationProvider> ConfigureFromJson(string configuration, IDirectory templateRoot)
        {
            JObject rawConfiguration = JObject.Parse(configuration);
            string? original = rawConfiguration.ToString("original");
            string? replacement = rawConfiguration.ToString("replacement");
            string? id = rawConfiguration.ToString("id");
            bool onByDefault = rawConfiguration.ToBool("onByDefault");

            JArray? onlyIf = rawConfiguration.Get<JArray>("onlyIf");
            TokenConfig coreConfig = original.TokenConfigBuilder();

            if (onlyIf != null)
            {
                foreach (JToken entry in onlyIf.Children())
                {
                    if (!(entry is JObject x))
                    {
                        continue;
                    }

                    string? before = entry.ToString("before");
                    string? after = entry.ToString("after");
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

        internal static IOperationProvider? Setup(IEngineEnvironmentSettings environmentSettings, IReplacementTokens tokens, IVariableCollection variables)
        {
            if (variables.TryGetValue(tokens.VariableName, out object newValueObject))
            {
                if (newValueObject is null)
                {
                    return null;
                }
                string newValue = newValueObject.ToString();
                return new Replacement(tokens.OriginalValue, newValue, null, true);
            }
            else
            {
                environmentSettings.Host.Logger.LogDebug($"Couldn't find a variable called {tokens.VariableName}");
                return null;
            }
        }
    }
}
