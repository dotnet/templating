// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Utils;
using Microsoft.TemplateEngine.Cli.TemplateResolution;
using System.Text;

namespace Microsoft.TemplateEngine.Cli.HelpAndUsage
{
    internal static class HelpForTemplateResolution
    {
        public static CreationResultStatus CoordinateHelpAndUsageDisplay(ListOrHelpTemplateListResolutionResult templateResolutionResult, IEngineEnvironmentSettings environmentSettings, INewCommandInput commandInput, IHostSpecificDataLoader hostDataLoader, ITelemetryLogger telemetryLogger, TemplateCreator templateCreator, string defaultLanguage, bool showUsageHelp = true)
        {
            if (showUsageHelp)
            {
                ShowUsageHelp(commandInput, telemetryLogger);
            }

            // this is just checking if there is an unambiguous group.
            // the called methods decide whether to get the default language filtered lists, based on what they're doing.
            //
            // The empty TemplateName check is for when only 1 template (or group) is installed.
            // When that occurs, the group is considered partial matches. But the output should be the ambiguous case - list the templates, not help on the singular group.
            if (!string.IsNullOrEmpty(commandInput.TemplateName)
                //TODO: clarify usage of template groups with Kathleen first
       //             && templateResolutionResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyList<ITemplateMatchInfo> unambiguousTemplateGroup)
                    && templateResolutionResult.HasUnambigiousTemplateToUse
                    && TemplateListResolver.AreAllTemplatesSameLanguage(templateResolutionResult.ExactMatchedTemplates))
            {
                // This will often show detailed help on the template group, which only makes sense if they're all the same language.
                return DisplayHelpForUnambiguousTemplateGroup(templateResolutionResult, environmentSettings, commandInput, hostDataLoader, templateCreator, defaultLanguage);
            }
            else
            {
                return DisplayHelpForAmbiguousTemplateGroup(templateResolutionResult, environmentSettings, commandInput, hostDataLoader, telemetryLogger, defaultLanguage);
            }
        }

        private static CreationResultStatus DisplayHelpForUnambiguousTemplateGroup(ListOrHelpTemplateListResolutionResult templateResolutionResult, IEngineEnvironmentSettings environmentSettings, INewCommandInput commandInput, IHostSpecificDataLoader hostDataLoader, TemplateCreator templateCreator, string defaultLanguage)
        {
            IReadOnlyCollection<ITemplateMatchInfo> unambiguousTemplateGroupForDetailDisplay = templateResolutionResult.ExactMatchedTemplates;
            if (commandInput.IsListFlagSpecified)
            {
                //TODO: clarify usage of template groups with Kathleen first
                //because the list flag is present, don't display help for the template group, even though an unambiguous group was resolved.
                if (!AreAllParamsValidForAnyTemplateInList(unambiguousTemplateGroupForDetailDisplay)
                    && TemplateListResolver.FindHighestPrecedenceTemplateIfAllSameGroupIdentity(unambiguousTemplateGroupForDetailDisplay) != null)
                {
                    DisplayHelpForAcceptedParameters(commandInput.CommandName);
                    return CreationResultStatus.InvalidParamValues;
                }

                ShowTemplatesFoundMessage(commandInput.TemplateName, commandInput.Language, commandInput.TypeFilter, commandInput.BaselineName);
                DisplayTemplateList(unambiguousTemplateGroupForDetailDisplay, environmentSettings, commandInput.Language, defaultLanguage);
                // list flag specified, so no usage examples or detailed help
                return CreationResultStatus.Success;
            }
            else
            {
                // not in list context, but Unambiguous
                // this covers whether or not --help was input, they do the same thing in the unambiguous case
                return TemplateDetailedHelpForSingularTemplateGroup(unambiguousTemplateGroupForDetailDisplay, environmentSettings, commandInput, hostDataLoader, templateCreator);
            }
        }

        private static CreationResultStatus TemplateDetailedHelpForSingularTemplateGroup(IReadOnlyCollection<ITemplateMatchInfo> unambiguousTemplateGroup, IEngineEnvironmentSettings environmentSettings, INewCommandInput commandInput, IHostSpecificDataLoader hostDataLoader, TemplateCreator templateCreator)
        {
            // (scp 2017-09-06): parse errors probably can't happen in this context.
            foreach (string parseErrorMessage in unambiguousTemplateGroup.Where(x => x.HasParseError()).Select(x => x.GetParseError()).ToList())
            {
                Reporter.Error.WriteLine(parseErrorMessage.Bold().Red());
            }

            GetParametersInvalidForTemplatesInList(unambiguousTemplateGroup, out IReadOnlyList<string> invalidForAllTemplates, out IReadOnlyList<string> invalidForSomeTemplates);

            if (invalidForAllTemplates.Count > 0 || invalidForSomeTemplates.Count > 0)
            {
                DisplayInvalidParameters(invalidForAllTemplates);
                DisplayParametersInvalidForSomeTemplates(invalidForSomeTemplates, LocalizableStrings.SingleTemplateGroupPartialMatchSwitchesNotValidForAllMatches);
            }

            bool showImplicitlyHiddenParams = unambiguousTemplateGroup.Count > 1;
            TemplateDetailsDisplay.ShowTemplateGroupHelp(unambiguousTemplateGroup, environmentSettings, commandInput, hostDataLoader, templateCreator, showImplicitlyHiddenParams);

            return invalidForAllTemplates.Count > 0 || invalidForSomeTemplates.Count > 0
                ? CreationResultStatus.InvalidParamValues
                : CreationResultStatus.Success;
        }

        private static CreationResultStatus DisplayHelpForAmbiguousTemplateGroup(ListOrHelpTemplateListResolutionResult templateResolutionResult, IEngineEnvironmentSettings environmentSettings, INewCommandInput commandInput, IHostSpecificDataLoader hostDataLoader, ITelemetryLogger telemetryLogger, string defaultLanguage)
        {
            // The following occurs when:
            //      --alias <value> is specifed
            //      --help is specified
            //      template (group) can't be resolved
            if (!string.IsNullOrWhiteSpace(commandInput.Alias))
            {
                Reporter.Error.WriteLine(LocalizableStrings.InvalidInputSwitch.Bold().Red());
                Reporter.Error.WriteLine("  " + commandInput.TemplateParamInputFormat("--alias").Bold().Red());
                return CreationResultStatus.NotFound;
            }

            bool hasInvalidParameters = false;
            IReadOnlyCollection<ITemplateMatchInfo> templatesForDisplay = templateResolutionResult.ExactMatchedTemplates;
            GetParametersInvalidForTemplatesInList(templatesForDisplay, out IReadOnlyList<string> invalidForAllTemplates, out IReadOnlyList<string> invalidForSomeTemplates);
            if (invalidForAllTemplates.Any() || invalidForSomeTemplates.Any())
            {
                hasInvalidParameters = true;
                DisplayInvalidParameters(invalidForAllTemplates);
                DisplayParametersInvalidForSomeTemplates(invalidForSomeTemplates, LocalizableStrings.PartialTemplateMatchSwitchesNotValidForAllMatches);
            }

            if (templateResolutionResult.HasExactMatches)
            {
                ShowTemplatesFoundMessage(commandInput.TemplateName, commandInput.Language, commandInput.TypeFilter, commandInput.BaselineName);
                DisplayTemplateList(templatesForDisplay, environmentSettings, commandInput.Language, defaultLanguage);
            }
            else
            {
                ShowContextAndTemplateNameMismatchHelp(templateResolutionResult, commandInput.TemplateName, commandInput.Language, commandInput.TypeFilter, commandInput.BaselineName);
            }

            if (!commandInput.IsListFlagSpecified)
            {
                TemplateUsageHelp.ShowInvocationExamples(templateResolutionResult, hostDataLoader, commandInput.CommandName);
            }

            if (hasInvalidParameters)
            {
                return CreationResultStatus.NotFound;
            }
            else if (commandInput.IsListFlagSpecified || commandInput.IsHelpFlagSpecified)
            {
                return templateResolutionResult.HasExactMatches ? CreationResultStatus.Success :  CreationResultStatus.NotFound;
            }
            else
            {
                return CreationResultStatus.OperationNotSpecified;
            }
        }

        // Returns true if any of the input templates has a valid parameter parse result.
        private static bool AreAllParamsValidForAnyTemplateInList(IReadOnlyCollection<ITemplateMatchInfo> templateList)
        {
            bool anyValidTemplate = false;

            foreach (ITemplateMatchInfo templateInfo in templateList)
            {
                if (templateInfo.GetInvalidParameterNames().Count == 0)
                {
                    anyValidTemplate = true;
                    break;
                }
            }

            return anyValidTemplate;
        }

        private static void DisplayHelpForAcceptedParameters(string commandName)
        {
            Reporter.Error.WriteLine(string.Format(LocalizableStrings.RunHelpForInformationAboutAcceptedParameters, commandName).Bold().Red());
        }

        // Displays the list of templates in a table, one row per template group.
        //
        // The columns displayed are as follows:
        // Except where noted, the values are taken from the highest-precedence template in the group. The info could vary among the templates in the group, but shouldn't.
        // (There is no check that the info doesn't vary.)
        // - Template Name
        // - Short Name: displays the first short name from the highest precedence template in the group.
        // - Language: All languages supported by any template in the group are displayed, with the default language in brackets, e.g.: [C#]
        // - Tags
        private static void DisplayTemplateList(IReadOnlyCollection<ITemplateMatchInfo> templates, IEngineEnvironmentSettings environmentSettings, string language, string defaultLanguage)
        {
            IReadOnlyCollection<TemplateGroupForListDisplay> groupsForDisplay = GetTemplateGroupsForListDisplay(templates, language, defaultLanguage);

            HelpFormatter<TemplateGroupForListDisplay> formatter =
                HelpFormatter
                    .For(
                        environmentSettings,
                        groupsForDisplay,
                        columnPadding: 6,
                        headerSeparator: '-',
                        blankLineBetweenRows: false)
                    .DefineColumn(t => t.Name, LocalizableStrings.Templates, shrinkIfNeeded: true)
                    .DefineColumn(t => t.ShortName, LocalizableStrings.ShortName)
                    .DefineColumn(t => t.Languages, out object languageColumn, LocalizableStrings.Language)
                    .DefineColumn(t => t.Classifications, out object tagsColumn, LocalizableStrings.Tags)
                    .OrderByDescending(languageColumn, new NullOrEmptyIsLastStringComparer())
                    .OrderBy(tagsColumn);
            Reporter.Output.WriteLine(formatter.Layout());
        }

        private class TemplateGroupForListDisplay
        {
            public string Name { get; set; }
            public string ShortName { get; set; }
            public string Languages { get; set; }
            public string Classifications { get; set; }
        }

        private static IReadOnlyList<TemplateGroupForListDisplay> GetTemplateGroupsForListDisplay(IReadOnlyCollection<ITemplateMatchInfo> templateList, string language, string defaultLanguage)
        {
            IEnumerable<IGrouping<string, ITemplateMatchInfo>> grouped = templateList.GroupBy(x => x.Info.GroupIdentity, x => !string.IsNullOrEmpty(x.Info.GroupIdentity));
            List<TemplateGroupForListDisplay> templateGroupsForDisplay = new List<TemplateGroupForListDisplay>();

            foreach (IGrouping<string, ITemplateMatchInfo> grouping in grouped)
            {
                List<string> languageForDisplay = new List<string>();
                HashSet<string> uniqueLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string defaultLanguageDisplay = string.Empty;

                foreach (ITemplateMatchInfo template in grouping)
                {
                    if (template.Info.Tags != null && template.Info.Tags.TryGetValue("language", out ICacheTag languageTag))
                    {
                        foreach (string lang in languageTag.ChoicesAndDescriptions.Keys)
                        {
                            if (uniqueLanguages.Add(lang))
                            {
                                if (string.IsNullOrEmpty(language) && string.Equals(defaultLanguage, lang, StringComparison.OrdinalIgnoreCase))
                                {
                                    defaultLanguageDisplay = $"[{lang}]";
                                }
                                else
                                {
                                    languageForDisplay.Add(lang);
                                }
                            }
                        }
                    }
                }

                languageForDisplay.Sort(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(defaultLanguageDisplay))
                {
                    languageForDisplay.Insert(0, defaultLanguageDisplay);
                }

                ITemplateMatchInfo highestPrecedenceTemplate = grouping.OrderByDescending(x => x.Info.Precedence).First();
                string shortName;
                if (highestPrecedenceTemplate.Info is IShortNameList highestWithShortNameList)
                {
                    shortName = highestWithShortNameList.ShortNameList[0];
                }
                else
                {
                    shortName = highestPrecedenceTemplate.Info.ShortName;
                }

                TemplateGroupForListDisplay groupDisplayInfo = new TemplateGroupForListDisplay()
                {
                    Name = highestPrecedenceTemplate.Info.Name,
                    ShortName = shortName,
                    Languages = string.Join(", ", languageForDisplay),
                    Classifications = highestPrecedenceTemplate.Info.Classifications != null ? string.Join("/", highestPrecedenceTemplate.Info.Classifications) : null
                };
                templateGroupsForDisplay.Add(groupDisplayInfo);
            }

            return templateGroupsForDisplay;
        }

        public static void DisplayInvalidParameters(IReadOnlyList<string> invalidParams)
        {
            if (invalidParams.Count > 0)
            {
                Reporter.Error.WriteLine(LocalizableStrings.InvalidInputSwitch.Bold().Red());
                foreach (string flag in invalidParams)
                {
                    Reporter.Error.WriteLine($"  {flag}".Bold().Red());
                }
            }
        }

        private static void DisplayParametersInvalidForSomeTemplates(IReadOnlyList<string> invalidParams, string messageHeader)
        {
            if (invalidParams.Count > 0)
            {
                Reporter.Error.WriteLine(messageHeader.Bold().Red());
                foreach (string flag in invalidParams)
                {
                    Reporter.Error.WriteLine($"  {flag}".Bold().Red());
                }
            }
        }

        private static void ShowContextAndTemplateNameMismatchHelp(ListOrHelpTemplateListResolutionResult templateResolutionResult, string templateName, string templateLanguage, string context, string baselineName)
        {
            if (string.IsNullOrEmpty(templateName) && string.IsNullOrEmpty(templateLanguage) && string.IsNullOrEmpty(context) && string.IsNullOrEmpty(baselineName))
            {
                return;
            }
            DisplayPartialNameMatchLanguageAndContextProblems(templateName, templateLanguage, context, templateResolutionResult, baselineName);
        }

        private static void DisplayPartialNameMatchLanguageAndContextProblems(string templateName, string templateLanguage, string context, ListOrHelpTemplateListResolutionResult templateResolutionResult, string baselineName)
        {
            bool anythingReported = false;
            if (templateResolutionResult.HasExactMatches)
            {
                return;
            }
            else
            {
                //
                ShowNoTemplatesFoundMessage(templateName, templateLanguage, context, baselineName);
                anythingReported = true;
            }

            if (templateResolutionResult.HasPartialMatches)
            {
                // {0} template(s) partially matched, but failed on {1}.
                Reporter.Error.WriteLine(
                    string.Format(
                        LocalizableStrings.TemplatesNotValidGivenTheSpecifiedFilter,
                        templateResolutionResult.PartiallyMatchedTemplates.Count,
                        GetPartialMatchReason(templateResolutionResult, templateLanguage, context, baselineName))
                    .Bold().Red());

                anythingReported = true;
            }

            if (anythingReported)
            {
                Reporter.Error.WriteLine();
            }
        }

        // Returns a list of the parameter names that are invalid for every template in the input group.
        public static void GetParametersInvalidForTemplatesInList(IReadOnlyCollection<ITemplateMatchInfo> templateList, out IReadOnlyList<string> invalidForAllTemplates, out IReadOnlyList<string> invalidForSomeTemplates)
        {
            IDictionary<string, int> invalidCounts = new Dictionary<string, int>();

            foreach (ITemplateMatchInfo template in templateList)
            {
                foreach (string paramName in template.GetInvalidParameterNames())
                {
                    if (!invalidCounts.ContainsKey(paramName))
                    {
                        invalidCounts[paramName] = 1;
                    }
                    else
                    {
                        invalidCounts[paramName]++;
                    }
                }
            }

            IEnumerable<IGrouping<string, string>> countGroups = invalidCounts.GroupBy(x => x.Value == templateList.Count ? "all" : "some", x => x.Key);
            invalidForAllTemplates = countGroups.FirstOrDefault(x => string.Equals(x.Key, "all", StringComparison.Ordinal))?.ToList();
            if (invalidForAllTemplates == null)
            {
                invalidForAllTemplates = new List<string>();
            }

            invalidForSomeTemplates = countGroups.FirstOrDefault(x => string.Equals(x.Key, "some", StringComparison.Ordinal))?.ToList();
            if (invalidForSomeTemplates == null)
            {
                invalidForSomeTemplates = new List<string>();
            }
        }

        public static void ShowUsageHelp(INewCommandInput commandInput, ITelemetryLogger telemetryLogger)
        {
            if (commandInput.IsHelpFlagSpecified)
            {
                telemetryLogger.TrackEvent(commandInput.CommandName + "-Help");
            }

            Reporter.Output.WriteLine(commandInput.HelpText);
            Reporter.Output.WriteLine();
        }

        public static CreationResultStatus HandleParseError(INewCommandInput commandInput, ITelemetryLogger telemetryLogger)
        {
            TemplateListResolver.ValidateRemainingParameters(commandInput, out IReadOnlyList<string> invalidParams);
            DisplayInvalidParameters(invalidParams);

            // TODO: get a meaningful error message from the parser
            if (commandInput.IsHelpFlagSpecified)
            {
                // this code path doesn't go through the full help & usage stack, so needs it's own call to ShowUsageHelp().
                ShowUsageHelp(commandInput, telemetryLogger);
            }
            else
            {
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.RunHelpForInformationAboutAcceptedParameters, commandInput.CommandName).Bold().Red());
            }

            return CreationResultStatus.InvalidParamValues;
        }

        private static string GetInputParametersString(string templateName, string templateLanguage, string context, string baselineName)
        {
            List<string> inputParametersList = new List<string>();
            if (!string.IsNullOrEmpty(templateName)){
                inputParametersList.Add("'" + templateName + "'");
            }
            if (!string.IsNullOrEmpty(templateLanguage)){
                inputParametersList.Add("lang='" + templateLanguage + "'");
            }
            if (!string.IsNullOrEmpty(context)){
                inputParametersList.Add("type=" + context + "'");
            }
            if (!string.IsNullOrEmpty(baselineName))
            {
                inputParametersList.Add("baseline=" + baselineName + "'");
            }
            return String.Join(", ", inputParametersList);
        }

        private static void ShowNoTemplatesFoundMessage(string templateName, string templateLanguage, string context, string baselineName)
        {
            // No templates found matching the following input parameter(s): {0}.
            Reporter.Error.WriteLine(string.Format(LocalizableStrings.NoTemplatesMatchingInputParameters, GetInputParametersString(templateName, templateLanguage, context, baselineName)).Bold().Red());
        }

        private static void ShowTemplatesFoundMessage(string templateName, string templateLanguage, string context, string baselineName)
        {
            if (!string.IsNullOrEmpty(templateName) || !string.IsNullOrEmpty(templateLanguage) || !string.IsNullOrEmpty(context) || !string.IsNullOrEmpty(baselineName))
            {
                // Templates found matching the following input parameter(s): {0}
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.TemplatesFoundMatchingInputParameters, GetInputParametersString(templateName, templateLanguage, context, baselineName)));
                Reporter.Output.WriteLine();
            }
        }

        private static string GetPartialMatchReason(ListOrHelpTemplateListResolutionResult templateResolutionResult, string templateLanguage, string context, string baselineName)
        {
            StringBuilder reason = new StringBuilder();
            string separator = ", ";

            if (templateResolutionResult.HasLanguageMismatch)
            {
                reason.AppendFormat("language='{0}'", templateLanguage);
            }
            if (templateResolutionResult.HasContextMismatch)
            {
                if (reason.Length != 0)
                {
                    reason.Append(separator);
                }
                reason.AppendFormat("type='{0}'", context);
            }
            if (templateResolutionResult.HasBaselineMismatch)
            {
                if (reason.Length != 0)
                {
                    reason.Append(separator);
                }
                reason.AppendFormat("baseline='{0}'", baselineName);
            }

            return reason.ToString();
        }
    }
}
