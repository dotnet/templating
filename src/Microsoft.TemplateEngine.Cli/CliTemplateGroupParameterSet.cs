// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli
{
    internal class CliTemplateGroupParameterSet
    {
        private readonly IReadOnlyList<IParameterSet> _parameterSetList;

        private IEnumerable<CliTemplateParameter>? _parameterDefinitions;

        private IDictionary<CliTemplateParameter, object>? _resolvedValues;

        internal CliTemplateGroupParameterSet(IReadOnlyList<IParameterSet> parameterSetList)
        {
            _parameterSetList = parameterSetList;
        }

        internal IEnumerable<CliTemplateParameter> ParameterDefinitions
        {
            get
            {
                if (_parameterDefinitions == null)
                {
                    IDictionary<string, ITemplateParameter> combinedParams = new Dictionary<string, ITemplateParameter>();
                    IDictionary<string, Dictionary<string, ParameterChoice>> combinedChoices = new Dictionary<string, Dictionary<string, ParameterChoice>>();

                    // gather info
                    foreach (IParameterSet paramSet in _parameterSetList)
                    {
                        foreach (ITemplateParameter parameter in paramSet.ParameterDefinitions)
                        {
                            // add the parameter to the combined list
                            if (!combinedParams.ContainsKey(parameter.Name))
                            {
                                combinedParams.Add(parameter.Name, parameter);
                            }

                            // build the combined choice lists
                            if (parameter.Choices != null)
                            {
                                Dictionary<string, ParameterChoice>? combinedChoicesForParam;
                                if (!combinedChoices.TryGetValue(parameter.Name, out combinedChoicesForParam))
                                {
                                    combinedChoicesForParam = new Dictionary<string, ParameterChoice>();
                                    combinedChoices.Add(parameter.Name, combinedChoicesForParam);
                                }

                                foreach (KeyValuePair<string, ParameterChoice> choiceAndDescription in parameter.Choices)
                                {
                                    if (!combinedChoicesForParam.ContainsKey(choiceAndDescription.Key))
                                    {
                                        combinedChoicesForParam[choiceAndDescription.Key] = choiceAndDescription.Value;
                                    }
                                }
                            }
                        }
                    }

                    // create the combined params
                    IList<CliTemplateParameter> outputParams = new List<CliTemplateParameter>();
                    foreach (KeyValuePair<string, ITemplateParameter> paramInfo in combinedParams)
                    {
                        if (!string.Equals(paramInfo.Value.DataType, "choice", StringComparison.OrdinalIgnoreCase))
                        {
                            CliTemplateParameter combinedParameter = new CliTemplateParameter(paramInfo.Value.Name)
                            {
                                Documentation = paramInfo.Value.Documentation,
                                Priority = paramInfo.Value.Priority,
                                DefaultValue = paramInfo.Value.DefaultValue,
                                DataType = paramInfo.Value.DataType,
                                DefaultIfOptionWithoutValue = paramInfo.Value.DefaultIfOptionWithoutValue
                            };
                            outputParams.Add(combinedParameter);
                        }
                        else
                        {
                            Dictionary<string, ParameterChoice>? choicesAndDescriptions;
                            if (!combinedChoices.TryGetValue(paramInfo.Key, out choicesAndDescriptions))
                            {
                                choicesAndDescriptions = new Dictionary<string, ParameterChoice>();
                            }

                            CliTemplateParameter combinedParameter = new CliTemplateParameter(paramInfo.Value.Name)
                            {
                                Documentation = paramInfo.Value.Documentation,
                                Priority = paramInfo.Value.Priority,
                                DefaultValue = paramInfo.Value.DefaultValue,
                                DataType = paramInfo.Value.DataType,
                                Choices = choicesAndDescriptions,
                                DefaultIfOptionWithoutValue = paramInfo.Value.DefaultIfOptionWithoutValue
                            };

                            outputParams.Add(combinedParameter);
                        }
                    }

                    _parameterDefinitions = outputParams;
                }

                return _parameterDefinitions;
            }
        }

        internal IDictionary<CliTemplateParameter, object> ResolvedValues
        {
            get
            {
                if (_resolvedValues == null)
                {
                    IDictionary<CliTemplateParameter, object> resolvedValues = new Dictionary<CliTemplateParameter, object>();

                    foreach (CliTemplateParameter groupParameter in ParameterDefinitions)
                    {
                        // take the first value from the first group that has a a value for this parameter.
                        foreach (IParameterSet baseParamSet in _parameterSetList)
                        {
                            ITemplateParameter? baseParam = baseParamSet.ParameterDefinitions.FirstOrDefault(x => string.Equals(x.Name, groupParameter.Name, StringComparison.OrdinalIgnoreCase));
                            if (baseParam != null)
                            {
                                if (baseParamSet.ResolvedValues.TryGetValue(baseParam, out object? value))
                                {
                                    resolvedValues.Add(groupParameter, value);
                                    break;  // from the inner loop
                                }
                            }
                        }
                    }

                    _resolvedValues = resolvedValues;
                }

                return _resolvedValues;
            }
        }

        internal bool TryGetParameterDefinition(string name, out CliTemplateParameter? parameter)
        {
            parameter = ParameterDefinitions.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

            if (parameter != null)
            {
                return true;
            }

            parameter = new CliTemplateParameter(name)
            {
                Documentation = string.Empty,
                Priority = TemplateParameterPriority.Optional,
                DefaultValue = string.Empty,
                DataType = "string",
                Choices = null,
            };

            return true;
        }

        internal bool TryGetRuntimeValue(IEngineEnvironmentSettings environmentSettings, string name, out object? value, bool skipEnvironmentVariableSearch = false)
        {
            if (TryGetParameterDefinition(name, out CliTemplateParameter? param) && param != null
                && ResolvedValues.TryGetValue(param, out object? newValueObject)
                && newValueObject != null)
            {
                value = newValueObject;
                return true;
            }

            if ((environmentSettings.Host.TryGetHostParamDefault(name, out string? newValue) && newValue != null)
                || (!skipEnvironmentVariableSearch && environmentSettings.Environment.GetEnvironmentVariables().TryGetValue(name, out newValue) && newValue != null))
            {
                value = newValue;
                return true;
            }

            value = null;
            return false;
        }
    }
}
