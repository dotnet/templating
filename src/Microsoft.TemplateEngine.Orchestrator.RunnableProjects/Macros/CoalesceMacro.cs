// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Abstractions.Parameters;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros.Config;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros
{
    internal class CoalesceMacro : IMacro, IDeferredMacro
    {
        public string Type => "coalesce";

        public Guid Id => new Guid("11C6EACF-8D24-42FD-8FC6-84063FCD8F14");

        public IMacroConfig CreateConfig(IEngineEnvironmentSettings environmentSettings, IMacroConfig rawConfig)
        {
            if (rawConfig is not GeneratedSymbolDeferredMacroConfig deferredConfig)
            {
                throw new InvalidCastException("Couldn't cast the rawConfig as a GeneratedSymbolDeferredMacroConfig");
            }

            string? sourceVariableName = null;
            if (deferredConfig.Parameters.TryGetValue("sourceVariableName", out JToken sourceVariableToken) && sourceVariableToken.Type == JTokenType.String)
            {
                sourceVariableName = sourceVariableToken.ToString();
            }

            string? defaultValue = null;
            if (deferredConfig.Parameters.TryGetValue("defaultValue", out JToken defaultValueToken) && defaultValueToken.Type == JTokenType.String)
            {
                defaultValue = defaultValueToken.ToString();
            }

            string? fallbackVariableName = null;
            if (deferredConfig.Parameters.TryGetValue("fallbackVariableName", out JToken fallbackVariableNameToken) && fallbackVariableNameToken.Type == JTokenType.String)
            {
                fallbackVariableName = fallbackVariableNameToken.ToString();
            }

            IMacroConfig realConfig = new CoalesceMacroConfig(deferredConfig.VariableName, deferredConfig.DataType, sourceVariableName, defaultValue, fallbackVariableName);
            return realConfig;
        }

        public void EvaluateConfig(IEngineEnvironmentSettings environmentSettings, IVariableCollection vars, IMacroConfig config)
        {
            if (config is not CoalesceMacroConfig realConfig)
            {
                throw new InvalidCastException("Unable to cast config as a CoalesceMacroConfig");
            }

			if (vars.TryGetValue(realConfig.SourceVariableName, out object currentSourceValue) && currentSourceValue != null)
            {
                // The value is equal to the coalesce recognized default value (see coalesce macro doc for details).
                if (realConfig.DefaultValue != null && currentSourceValue.ToString().Equals(realConfig.DefaultValue))
                {
                    environmentSettings.Host.Logger.LogDebug("[{macro}]: '{var}': source value '{source}' is not used, because it is equal to default value '{default}'.", nameof(CoalesceMacro), realConfig.VariableName, currentSourceValue, realConfig.DefaultValue);
                }
                // The value is not specified by user: either coming from default value or host specific default value, etc.
                else if (vars is ParameterBasedVariableCollection paramsVariableCollection &&
                    paramsVariableCollection.ParameterSetData.TryGetValue(realConfig.SourceVariableName, out ParameterData? parameterData) &&
                    parameterData!.DataSource is not DataSource.User and not DataSource.DefaultIfNoValue)
                {
                    environmentSettings.Host.Logger.LogDebug(
                        "[{macro}]: '{var}': source value '{source}' not specified by user (data source: '{dataSource}'), fall back.",
                        nameof(CoalesceMacro),
                        realConfig.VariableName,
                        currentSourceValue,
                        parameterData.DataSource);
                }
                else if (currentSourceValue is string str && string.IsNullOrEmpty(str))
                {
                    //do nothing, empty value for string is equivalent to null.
                    environmentSettings.Host.Logger.LogDebug("[{macro}]: '{var}': source value '{source}' is an empty string, fall back.", nameof(CoalesceMacro), realConfig.VariableName, currentSourceValue);
                }
                else
                {
                    vars[realConfig.VariableName] = currentSourceValue;
                    environmentSettings.Host.Logger.LogDebug("[{macro}]: Assigned variable '{var}' to '{value}'.", nameof(CoalesceMacro), realConfig.VariableName, currentSourceValue);
                    return;
                }
            }
            if (vars.TryGetValue(realConfig.FallbackVariableName, out object currentFallbackValue) && currentFallbackValue != null)
            {
                vars[realConfig.VariableName] = currentFallbackValue;
                environmentSettings.Host.Logger.LogDebug("[{macro}]: Assigned variable '{var}' to fallback value '{value}'.", nameof(CoalesceMacro), realConfig.VariableName, currentFallbackValue);
                return;
            }
            environmentSettings.Host.Logger.LogDebug("[{macro}]: Variable '{var}' was not assigned, neither source nor fallback variable was found.", nameof(CoalesceMacro), realConfig.VariableName);
        }
    }
}
