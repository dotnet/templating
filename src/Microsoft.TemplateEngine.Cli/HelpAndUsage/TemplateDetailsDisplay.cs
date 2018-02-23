// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Utils;
using Microsoft.TemplateEngine.Cli.CommandParsing;

namespace Microsoft.TemplateEngine.Cli.HelpAndUsage
{
    public static class TemplateDetailsDisplay
    {
        public static void ShowTemplateGroupHelp(IReadOnlyList<ITemplateMatchInfo> templateGroup, IEngineEnvironmentSettings environmentSettings, INewCommandInput commandInput, IHostSpecificDataLoader hostDataLoader, TemplateCreator templateCreator, bool showImplicitlyHiddenParams = false)
        {
            if (templateGroup.Count == 0 || !TemplateListResolver.AreAllTemplatesSameGroupIdentity(templateGroup))
            {
                return;
            }

            IReadOnlyList<ITemplateInfo> templateInfoList = templateGroup.Select(x => x.Info).ToList();
            ShowTemplateDetailHeaders(templateInfoList);

            TemplateGroupParameterDetails groupParameterDetails = DetermineParameterDispositionsForTemplateGroup(templateInfoList, environmentSettings, commandInput, hostDataLoader, templateCreator);

            // get the input params valid for any param in the group
            IReadOnlyDictionary<string, string> inputTemplateParams = CoalesceInputParameterValuesFromTemplateGroup(templateGroup);

            ShowParameterHelp(inputTemplateParams, showImplicitlyHiddenParams, groupParameterDetails, environmentSettings);
        }

        private static void ShowTemplateDetailHeaders(IReadOnlyList<ITemplateInfo> templateGroup)
        {
            // Use the highest precedence template for most of the output
            ITemplateInfo preferredTemplate = templateGroup.OrderByDescending(x => x.Precedence).First();

            // use all templates to get the language choices
            HashSet<string> languages = new HashSet<string>();
            foreach (ITemplateInfo templateInfo in templateGroup)
            {
                if (templateInfo.Tags != null && templateInfo.Tags.TryGetValue("language", out ICacheTag languageTag))
                {
                    languages.UnionWith(languageTag.ChoicesAndDescriptions.Keys.Where(x => !string.IsNullOrWhiteSpace(x)).ToList());
                }
            }

            if (languages != null && languages.Any())
            {
                Reporter.Output.WriteLine($"{preferredTemplate.Name} ({string.Join(", ", languages)})");
            }
            else
            {
                Reporter.Output.WriteLine(preferredTemplate.Name);
            }

            if (!string.IsNullOrWhiteSpace(preferredTemplate.Author))
            {
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.Author, preferredTemplate.Author));
            }

            if (!string.IsNullOrWhiteSpace(preferredTemplate.Description))
            {
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.Description, preferredTemplate.Description));
            }

            if (!string.IsNullOrEmpty(preferredTemplate.ThirdPartyNotices))
            {
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.ThirdPartyNotices, preferredTemplate.ThirdPartyNotices));
            }
        }

        private static void ShowParameterHelp(IReadOnlyDictionary<string, string> inputParams, bool showImplicitlyHiddenParams, TemplateGroupParameterDetails parameterDetails, IEngineEnvironmentSettings environmentSettings)
        {
            if (!string.IsNullOrEmpty(parameterDetails.AdditionalInfo))
            {
                Reporter.Error.WriteLine(parameterDetails.AdditionalInfo.Bold().Red());
                Reporter.Output.WriteLine();
            }

            IEnumerable<ITemplateParameter> filteredParams = TemplateParameterHelpBase.FilterParamsForHelp(parameterDetails.AllParams.ParameterDefinitions, parameterDetails.ExplicitlyHiddenParams,
                                                                                    showImplicitlyHiddenParams, parameterDetails.HasPostActionScriptRunner, parameterDetails.ParametersToAlwaysShow);
            bool anyParamsShowingDefaultForSwitchWithNoValue = false;

            if (filteredParams.Any())
            {
                HelpFormatter<ITemplateParameter> formatter = new HelpFormatter<ITemplateParameter>(environmentSettings, filteredParams, 2, null, true);

                formatter.DefineColumn(
                    param =>
                    {
                        string options;
                        if (string.Equals(param.Name, "allow-scripts", StringComparison.OrdinalIgnoreCase))
                        {
                            options = "--" + param.Name;
                        }
                        else
                        {
                            // the key is guaranteed to exist
                            IList<string> variants = parameterDetails.GroupVariantsForCanonicals[param.Name].ToList();
                            options = string.Join("|", variants.Reverse());
                        }

                        return "  " + options;
                    },
                    LocalizableStrings.Options
                );

                formatter.DefineColumn(delegate (ITemplateParameter param)
                {
                    StringBuilder displayValue = new StringBuilder(255);
                    displayValue.AppendLine(param.Documentation);

                    if (string.Equals(param.DataType, "choice", StringComparison.OrdinalIgnoreCase))
                    {
                        int longestChoiceLength = param.Choices.Keys.Max(x => x.Length);

                        foreach (KeyValuePair<string, string> choiceInfo in param.Choices)
                        {
                            displayValue.Append("    " + choiceInfo.Key.PadRight(longestChoiceLength + 4));

                            if (!string.IsNullOrWhiteSpace(choiceInfo.Value))
                            {
                                displayValue.Append("- " + choiceInfo.Value);
                            }

                            displayValue.AppendLine();
                        }
                    }
                    else
                    {
                        displayValue.Append(param.DataType ?? "string");
                        displayValue.AppendLine(" - " + param.Priority.ToString());
                    }

                    // determine the configured value
                    string configuredValue = null;
                    if (parameterDetails.AllParams.ResolvedValues.TryGetValue(param, out object resolvedValueObject))
                    {
                        // Set the configured value as long as it's non-empty and not the default value.
                        // If it's the default, we're not sure if it was explicitly entered or not.
                        // Below, there's a check if the user entered a value. If so, set it.
                        string resolvedValue = resolvedValueObject?.ToString() ?? string.Empty;

                        if (!string.IsNullOrEmpty(resolvedValue))
                        {
                            if (string.Equals(param.DataType, "bool", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!string.Equals(param.DefaultValue, resolvedValue, StringComparison.OrdinalIgnoreCase))
                                {
                                    // bools get ToString() values of "True" & "False", but most templates use "true" & "false"
                                    // So do a case-insensitive comparison on bools.
                                    configuredValue = resolvedValue;
                                }
                            }
                            else if (!string.Equals(param.DefaultValue, resolvedValue, StringComparison.Ordinal))
                            {
                                // other datatypes need a case-sensitive comparison.
                                configuredValue = resolvedValue;
                            }
                        }
                    }

                    // If the resolved value is null/empty, or the default, only display the configured value if
                    // the user explicitly specified it or if it can be resolved from the DefaultIfOptionWithoutValue (or bool='true' for backwards compat).
                    if (string.IsNullOrEmpty(configuredValue))
                    {
                        bool handled = false;

                        if (parameterDetails.GroupUserParamsWithDefaultValues.Contains(param.Name))
                        {
                            if (param is IAllowDefaultIfOptionWithoutValue parameterWithNoValueDefault)
                            {
                                if (!string.IsNullOrEmpty(parameterWithNoValueDefault.DefaultIfOptionWithoutValue))
                                {
                                    configuredValue = parameterWithNoValueDefault.DefaultIfOptionWithoutValue;
                                    handled = true;
                                }
                            }
                            else if (string.Equals(param.DataType, "bool", StringComparison.OrdinalIgnoreCase))
                            {
                                // Before the introduction of DefaultIfOptionWithoutValue, bool params were handled like this.
                                // Other param types didn't have any analogous handling.
                                configuredValue = "true";
                                handled = true;
                            }
                        }

                        if (!handled)
                        {
                            // If the user explicitly specified the switch, the value is in the inputParams, so try to retrieve it here.
                            inputParams.TryGetValue(param.Name, out configuredValue);
                        }
                    }

                    // display the configured value if there is one
                    if (!string.IsNullOrEmpty(configuredValue))
                    {
                        string realValue = configuredValue;

                        if (parameterDetails.InvalidParams.Contains(param.Name) ||
                            (string.Equals(param.DataType, "choice", StringComparison.OrdinalIgnoreCase)
                                && !param.Choices.ContainsKey(configuredValue)))
                        {
                            realValue = realValue.Bold().Red();
                        }
                        else if (parameterDetails.AllParams.TryGetRuntimeValue(environmentSettings, param.Name, out object runtimeVal) && runtimeVal != null)
                        {
                            realValue = runtimeVal.ToString();
                        }

                        displayValue.AppendLine(string.Format(LocalizableStrings.ConfiguredValue, realValue));
                    }

                    // display the default value if there is one
                    if (!string.IsNullOrEmpty(param.DefaultValue))
                    {
                        if (param is IAllowDefaultIfOptionWithoutValue paramWithNoValueDefault
                            && !string.IsNullOrEmpty(paramWithNoValueDefault.DefaultIfOptionWithoutValue)
                            && !string.Equals(paramWithNoValueDefault.DefaultIfOptionWithoutValue, param.DefaultValue, StringComparison.Ordinal))
                        {
                            // the regular default, and the default if the switch is provided without a value
                            displayValue.AppendLine(string.Format(LocalizableStrings.DefaultValuePlusSwitchWithoutValueDefault, param.DefaultValue, paramWithNoValueDefault.DefaultIfOptionWithoutValue));
                            anyParamsShowingDefaultForSwitchWithNoValue = true;
                        }
                        else
                        {
                            // only the regular default
                            displayValue.AppendLine(string.Format(LocalizableStrings.DefaultValue, param.DefaultValue));
                        }
                    }

                    return displayValue.ToString();
                }, string.Empty);

                Reporter.Output.WriteLine(formatter.Layout());

                if (anyParamsShowingDefaultForSwitchWithNoValue)
                {
                    Reporter.Output.WriteLine(LocalizableStrings.SwitchWithoutValueDefaultFootnote);
                }
            }
            else
            {
                Reporter.Output.WriteLine(LocalizableStrings.NoParameters);
            }
        }

        // Returns a composite of the input parameters and values which are valid for any template in the group.
        private static IReadOnlyDictionary<string, string> CoalesceInputParameterValuesFromTemplateGroup(IReadOnlyList<ITemplateMatchInfo> templateGroup)
        {
            Dictionary<string, string> inputValues = new Dictionary<string, string>();

            foreach (ITemplateMatchInfo template in templateGroup.OrderBy(x => x.Info.Precedence))
            {
                foreach (KeyValuePair<string, string> paramAndValue in template.GetValidTemplateParameters())
                {
                    inputValues[paramAndValue.Key] = paramAndValue.Value;
                }
            }

            return inputValues;
        }

        private static TemplateGroupParameterDetails DetermineParameterDispositionsForTemplateGroup(IReadOnlyList<ITemplateInfo> templateGroup, IEngineEnvironmentSettings environmentSettings, INewCommandInput commandInput, IHostSpecificDataLoader hostDataLoader, TemplateCreator templateCreator)
        {
            HashSet<string> groupUserParamsWithInvalidValues = new HashSet<string>(StringComparer.Ordinal);
            bool groupHasPostActionScriptRunner = false;
            List<IParameterSet> parameterSetsForAllTemplatesInGroup = new List<IParameterSet>();
            IDictionary<string, InvalidParameterInfo> invalidParametersForGroup = new Dictionary<string, InvalidParameterInfo>(StringComparer.Ordinal);
            bool firstInList = true;

            Dictionary<string, IReadOnlyList<string>> defaultVariantsForCanonicals = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            Dictionary<string, IReadOnlyList<string>> groupVariantsForCanonicals = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

            HashSet<string> groupUserParamsWithDefaultValues = new HashSet<string>(StringComparer.Ordinal);
            Dictionary<string, bool> parameterHidingDisposition = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> parametersToAlwaysShow = new HashSet<string>(StringComparer.Ordinal);

            foreach (ITemplateInfo templateInfo in templateGroup.OrderByDescending(x => x.Precedence))
            {
                TemplateUsageInformation usageInformation = TemplateUsageHelp.GetTemplateUsageInformation(templateInfo, environmentSettings, commandInput, hostDataLoader, templateCreator);
                HostSpecificTemplateData hostSpecificTemplateData = hostDataLoader.ReadHostSpecificTemplateData(templateInfo);
                HashSet<string> parametersToExplicitlyHide = hostSpecificTemplateData?.HiddenParameterNames ?? new HashSet<string>(StringComparer.Ordinal);

                foreach (ITemplateParameter parameter in usageInformation.AllParameters.ParameterDefinitions)
                {
                    //If the parameter has previously been encountered...
                    if (parameterHidingDisposition.TryGetValue(parameter.Name, out bool isCurrentlyHidden))
                    {
                        //...and it was hidden, but it's not hidden in this template in the group,
                        //  remove its hiding, otherwise leave it as is
                        if (isCurrentlyHidden && !parametersToExplicitlyHide.Contains(parameter.Name))
                        {
                            parameterHidingDisposition[parameter.Name] = false;
                        }
                    }
                    else
                    {
                        //...otherwise, since this is the first time the parameter has been seen,
                        //  its hiding state should be used as the current disposition
                        parameterHidingDisposition[parameter.Name] = parametersToExplicitlyHide.Contains(parameter.Name);
                    }
                }

                if (firstInList)
                {
                    invalidParametersForGroup = usageInformation.InvalidParameters.ToDictionary(x => x.Canonical, x => x);
                    firstInList = false;
                }
                else
                {
                    invalidParametersForGroup = InvalidParameterInfo.IntersectWithExisting(invalidParametersForGroup, usageInformation.InvalidParameters);
                }

                groupUserParamsWithInvalidValues.IntersectWith(usageInformation.UserParametersWithInvalidValues);    // intersect because if the value is valid for any version, it's valid.
                groupHasPostActionScriptRunner |= usageInformation.HasPostActionScriptRunner;
                parameterSetsForAllTemplatesInGroup.Add(usageInformation.AllParameters);

                // If this template has name overrides (either long or short), it's opinionated.
                //      If it's the first opinionated template about the param, use its variants.
                // Else this template is not opinionated, note its values if there aren't defaults for the param already.
                // At the end, anything in the default list that isn't in the opinionated list will get merged in.
                // TODO: write tests for this code (and the rest of this method while we're at it)
                foreach (KeyValuePair<string, IReadOnlyList<string>> canonicalAndVariants in usageInformation.VariantsForCanonicals)
                {
                    if (hostSpecificTemplateData.LongNameOverrides.ContainsKey(canonicalAndVariants.Key) || hostSpecificTemplateData.ShortNameOverrides.ContainsKey(canonicalAndVariants.Key))
                    {
                        // this template is opinionated about this parameter. If no previous template is opinionated about this param, use this template's variants.
                        if (!groupVariantsForCanonicals.ContainsKey(canonicalAndVariants.Key))
                        {
                            groupVariantsForCanonicals[canonicalAndVariants.Key] = canonicalAndVariants.Value;
                        }
                    }
                    else
                    {
                        // this template is not opinionated about this parameter. If no previous template had defaults for this param, use this template's defaults.
                        if (!defaultVariantsForCanonicals.ContainsKey(canonicalAndVariants.Key))
                        {
                            defaultVariantsForCanonicals[canonicalAndVariants.Key] = canonicalAndVariants.Value;
                        }
                    }
                }

                // If any template says the user input value is the default, include it here.
                groupUserParamsWithDefaultValues.UnionWith(usageInformation.UserParametersWithDefaultValues);
                parametersToAlwaysShow.UnionWith(hostSpecificTemplateData.ParametersToAlwaysShow);
            }

            // aggregate the parameter variants
            foreach (KeyValuePair<string, IReadOnlyList<string>> defaultVariants in defaultVariantsForCanonicals)
            {
                if (!groupVariantsForCanonicals.ContainsKey(defaultVariants.Key))
                {
                    // there were no opinionated variants, take the preferred default.
                    groupVariantsForCanonicals[defaultVariants.Key] = defaultVariants.Value;
                }
            }

            IParameterSet allGroupParameters = new TemplateGroupParameterSet(parameterSetsForAllTemplatesInGroup);
            string parameterErrors = InvalidParameterInfo.InvalidParameterListToString(invalidParametersForGroup.Values.ToList());
            HashSet<string> parametersToHide = new HashSet<string>(parameterHidingDisposition.Where(x => x.Value).Select(x => x.Key), StringComparer.Ordinal);

            return new TemplateGroupParameterDetails
            {
                AllParams = allGroupParameters,
                AdditionalInfo = parameterErrors,
                InvalidParams = groupUserParamsWithInvalidValues.ToList(),
                ExplicitlyHiddenParams = parametersToHide,
                GroupVariantsForCanonicals = groupVariantsForCanonicals,
                GroupUserParamsWithDefaultValues = groupUserParamsWithDefaultValues,
                HasPostActionScriptRunner = groupHasPostActionScriptRunner,
                ParametersToAlwaysShow = parametersToAlwaysShow,
            };
        }

        private struct TemplateGroupParameterDetails
        {
            public IParameterSet AllParams;
            public string AdditionalInfo;       // TODO: rename (probably)
            public IReadOnlyList<string> InvalidParams;
            public HashSet<string> ExplicitlyHiddenParams;
            public IReadOnlyDictionary<string, IReadOnlyList<string>> GroupVariantsForCanonicals;
            public HashSet<string> GroupUserParamsWithDefaultValues;
            public bool HasPostActionScriptRunner;
            public HashSet<string> ParametersToAlwaysShow;
        }
    }
}
