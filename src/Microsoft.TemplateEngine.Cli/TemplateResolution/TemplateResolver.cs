// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Cli.TemplateResolution
{
    public static class TemplateResolver
    {
        internal const string DefaultLanguageMatchParameterName = "DefaultLanguage";

        public static void ParseTemplateArgs(ITemplateInfo templateInfo, IHostSpecificDataLoader hostDataLoader, INewCommandInput commandInput)
        {
            HostSpecificTemplateData hostData = hostDataLoader.ReadHostSpecificTemplateData(templateInfo);
            commandInput.ReparseForTemplate(templateInfo, hostData);
        }

        public static bool AreAllTemplatesSameGroupIdentity(IEnumerable<ITemplateMatchInfo> templateList)
        {
            if (!templateList.Any())
            {
                return false;
            }

            return templateList.AllAreTheSame((x) => x.Info.GroupIdentity, StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsTemplateHiddenByHostFile(ITemplateInfo templateInfo, IHostSpecificDataLoader hostDataLoader)
        {
            HostSpecificTemplateData hostData = hostDataLoader.ReadHostSpecificTemplateData(templateInfo);
            return hostData.IsHidden;
        }

        // This version relies on the commandInput being in the context desired - so the most recent parse would have to have been
        // for what wants to be validated, either:
        //  - not in the context of any template
        //  - in the context of a specific template.
        public static bool ValidateRemainingParameters(INewCommandInput commandInput, out IReadOnlyList<string> invalidParams)
        {
            List<string> badParams = new List<string>();

            if (commandInput.RemainingParameters.Any())
            {
                foreach (string flag in commandInput.RemainingParameters.Keys)
                {
                    badParams.Add(flag);
                }
            }

            invalidParams = badParams;
            return !invalidParams.Any();
        }

        // This version is preferred, its clear which template the results are in the context of.
        public static bool ValidateRemainingParameters(ITemplateMatchInfo template, out IReadOnlyList<string> invalidParams)
        {
            invalidParams = template.GetInvalidParameterNames();

            return !invalidParams.Any();
        }

        // Lists all the templates, unfiltered - except the ones hidden by their host file.
        public static IReadOnlyCollection<ITemplateMatchInfo> PerformAllTemplatesQuery(IReadOnlyList<ITemplateInfo> templateInfo, IHostSpecificDataLoader hostDataLoader)
        {
            IReadOnlyList<FilterableTemplateInfo> filterableTemplateInfo = SetupFilterableTemplateInfoFromTemplateInfo(templateInfo);

            IReadOnlyCollection<ITemplateMatchInfo> templates = TemplateListFilter.GetTemplateMatchInfo(
                filterableTemplateInfo,
                TemplateListFilter.PartialMatchFilter,
                WellKnownSearchFilters.NameFilter(string.Empty)
            )
            .Where(x => !IsTemplateHiddenByHostFile(x.Info, hostDataLoader)).ToList();

            return templates;
        }

        public static TemplateResolutionResult GetTemplateResolutionResult(IReadOnlyList<ITemplateInfo> templateInfo, IHostSpecificDataLoader hostDataLoader, INewCommandInput commandInput, string defaultLanguage)
        {
            IReadOnlyCollection<ITemplateMatchInfo> coreMatchedTemplates = PerformCoreTemplateQuery(templateInfo, hostDataLoader, commandInput, defaultLanguage);
            return new TemplateResolutionResult(commandInput.Language, coreMatchedTemplates);
        }

        public static TemplateListResolutionResult GetTemplateResolutionResultForListOrHelp(IReadOnlyList<ITemplateInfo> templateInfo, IHostSpecificDataLoader hostDataLoader, INewCommandInput commandInput, string defaultLanguage)
        {
            IEnumerable<ITemplateMatchInfo> coreMatchedTemplates;

            //we need different set of templates for help and list
            //for list we need to show all exact and partial names by name
            //for help if there is an exact match by short name or name we need to show help for that exact template and also apply default language mapping in case language is not specified
            if (commandInput.IsListFlagSpecified)
            {
                coreMatchedTemplates = PerformCoreTemplateQueryForList(templateInfo, hostDataLoader, commandInput, defaultLanguage);
            }
            else
            {
                coreMatchedTemplates = PerformCoreTemplateQueryForHelp(templateInfo, hostDataLoader, commandInput, defaultLanguage);
            }
            return new TemplateListResolutionResult(coreMatchedTemplates.ToList());
        }

        public static IEnumerable<ITemplateMatchInfo> PerformCoreTemplateQueryForList(IEnumerable<ITemplateInfo> templateInfo, IHostSpecificDataLoader hostDataLoader, INewCommandInput commandInput, string defaultLanguage)
        {
            IReadOnlyList<FilterableTemplateInfo> filterableTemplateInfo = SetupFilterableTemplateInfoFromTemplateInfo(templateInfo);

            // for list we also try to get match on template name in classification (tags). These matches only will be used if short name and name has a mismatch.
            // filter below only sets the exact or partial match if name matches the tag. If name doesn't match the tag, no match disposition is added to collection.

            var listFilters = new List<Func<ITemplateInfo, MatchInfo?>>()
            {
                WellKnownSearchFilters.NameFilter(commandInput.TemplateName)
            };
            listFilters.AddRange(SupportedFilterOptions.SupportedListFilters
                                    .OfType<TemplateFilterOption>()
                                    .Select(filter => filter.TemplateMatchFilter(commandInput)));

            IEnumerable<ITemplateMatchInfo> coreMatchedTemplates = TemplateListFilter.GetTemplateMatchInfo(
                filterableTemplateInfo,
                TemplateListFilter.PartialMatchFilter,
                listFilters.ToArray()
            )
            .Where(x => !IsTemplateHiddenByHostFile(x.Info, hostDataLoader)).ToList();

            coreMatchedTemplates = AddParameterMatchingToTemplates(coreMatchedTemplates, hostDataLoader, commandInput);
            return coreMatchedTemplates;
        }

        public static IReadOnlyCollection<ITemplateMatchInfo> PerformCoreTemplateQueryForHelp(IReadOnlyList<ITemplateInfo> templateInfo, IHostSpecificDataLoader hostDataLoader, INewCommandInput commandInput, string defaultLanguage)
        {
            IReadOnlyList<FilterableTemplateInfo> filterableTemplateInfo = SetupFilterableTemplateInfoFromTemplateInfo(templateInfo);
            IReadOnlyList<ITemplateMatchInfo> coreMatchedTemplates = TemplateListFilter.GetTemplateMatchInfo(
                filterableTemplateInfo,
                TemplateListFilter.PartialMatchFilter,
                WellKnownSearchFilters.NameFilter(commandInput.TemplateName),
                WellKnownSearchFilters.LanguageFilter(commandInput.Language),
                WellKnownSearchFilters.TypeFilter(commandInput.TypeFilter),
                WellKnownSearchFilters.BaselineFilter(commandInput.BaselineName)
            )
            .Where(x => !IsTemplateHiddenByHostFile(x.Info, hostDataLoader)).ToList();

            //for help if template name from CLI exactly matches the template name we should consider only that template
            IReadOnlyList<ITemplateMatchInfo> matchesWithExactDispositionsInNameFields =
                coreMatchedTemplates.Where(
                    x => x.MatchDisposition.Any(
                        y =>
                            (y.ParameterName == MatchInfo.DefaultParameter.Name || y.ParameterName == MatchInfo.DefaultParameter.ShortName)
                            && y.Kind == MatchKind.Exact)).ToList();

            if (matchesWithExactDispositionsInNameFields.Count > 0)
            {
                coreMatchedTemplates = matchesWithExactDispositionsInNameFields;
            }

            //for help we also need to match on default language if language was not specified as parameter
            if (string.IsNullOrEmpty(commandInput.Language) && !string.IsNullOrEmpty(defaultLanguage))
            {
                // default language matching only makes sense if the user didn't specify a language.
                AddDefaultLanguageMatchingToTemplates(coreMatchedTemplates, defaultLanguage);
            }
            AddParameterMatchingToTemplates(coreMatchedTemplates, hostDataLoader, commandInput);
            return coreMatchedTemplates;
        }

        /// <summary>
        /// Performs filtering of provided template list for --search option. Filters applied: template name filter, --search option filters, template parameters filter.
        /// Only templates that exactly match the filters are returned.
        /// </summary>
        /// <param name="templateInfo">the list of templates to be filtered.</param>
        /// <param name="hostDataLoader">data of the host.</param>
        /// <param name="commandInput">new command data used in CLI.</param>
        /// <returns>filtered list of templates.</returns>
        public static IReadOnlyCollection<ITemplateMatchInfo> PerformCoreTemplateQueryForSearch(IEnumerable<ITemplateInfo> templateInfo, IHostSpecificDataLoader hostDataLoader, INewCommandInput commandInput)
        {
            IReadOnlyList<FilterableTemplateInfo> filterableTemplateInfo = SetupFilterableTemplateInfoFromTemplateInfo(templateInfo.ToList());
            List<Func<ITemplateInfo, MatchInfo?>> searchFilters = new List<Func<ITemplateInfo, MatchInfo?>>()
            {
                WellKnownSearchFilters.NameFilter(commandInput.TemplateName),
            };
            searchFilters.AddRange(SupportedFilterOptions.SupportedSearchFilters
                                    .OfType<TemplateFilterOption>()
                                    .Select(filter => filter.TemplateMatchFilter(commandInput)));

            IReadOnlyCollection<ITemplateMatchInfo> matchedTemplates = TemplateListFilter.GetTemplateMatchInfo(filterableTemplateInfo, TemplateListFilter.ExactMatchFilter, searchFilters.ToArray());

            AddParameterMatchingToTemplates(matchedTemplates, hostDataLoader, commandInput);
            return matchedTemplates.Where(t => t.IsInvokableMatch()).ToList();
        }

        /// <summary>
        /// Performs the filtering of installed templates for template instantiated.
        /// Filters applied: template name filter; language, type, classification and baseline filters. Only templates that match the filters are returned, no partial matches allowed.
        /// In case any templates in match above are matching name or short name exactly, only they are returned.
        /// The matches for default language and template specific parameters are added to the result.
        /// </summary>
        /// <param name="templateInfo">the list of templates to be filtered.</param>
        /// <param name="hostDataLoader">data of the host.</param>
        /// <param name="commandInput">new command data used in CLI.</param>
        /// <param name="defaultLanguage"></param>
        /// <returns>the collection of the templates with their match dispositions (<seealso cref="ITemplateMatchInfo"/>). The templates that do not match are not added to the collection.</returns>
        public static IReadOnlyCollection<ITemplateMatchInfo> PerformCoreTemplateQuery(IReadOnlyList<ITemplateInfo> templateInfo, IHostSpecificDataLoader hostDataLoader, INewCommandInput commandInput, string defaultLanguage)
        {
            IReadOnlyList<FilterableTemplateInfo> filterableTemplateInfo = SetupFilterableTemplateInfoFromTemplateInfo(templateInfo);

            IReadOnlyCollection<ITemplateMatchInfo> templates = TemplateListFilter.GetTemplateMatchInfo(
                filterableTemplateInfo,
                TemplateListFilter.ExactMatchFilter,
                WellKnownSearchFilters.NameFilter(commandInput.TemplateName),
                WellKnownSearchFilters.LanguageFilter(commandInput.Language),
                WellKnownSearchFilters.TypeFilter(commandInput.TypeFilter),
                WellKnownSearchFilters.BaselineFilter(commandInput.BaselineName)
            )
            .Where(x => !IsTemplateHiddenByHostFile(x.Info, hostDataLoader)).ToList();

            //select only the templates which do not have mismatches
            //if any template has exact match for name - use those; otherwise partial name matches are also considered when resolving templates
            IReadOnlyList<ITemplateMatchInfo> matchesWithExactDispositionsInNameFields =
                templates.Where(
                    x => x.MatchDisposition.Any(
                        y =>
                            (y.ParameterName == MatchInfo.DefaultParameter.Name || y.ParameterName == MatchInfo.DefaultParameter.ShortName)
                            && y.Kind == MatchKind.Exact)).ToList();

            if (matchesWithExactDispositionsInNameFields.Count > 0)
            {
                templates = matchesWithExactDispositionsInNameFields;
            }

            if (string.IsNullOrEmpty(commandInput.Language) && !string.IsNullOrEmpty(defaultLanguage))
            {
                // add default language matches to the list
                // default language matching only makes sense if the user didn't specify a language.
                AddDefaultLanguageMatchingToTemplates(templates, defaultLanguage);
            }

            //add specific template parameters matches to the list
            AddParameterMatchingToTemplates(templates, hostDataLoader, commandInput);

            return templates;
        }

        private static IReadOnlyList<FilterableTemplateInfo> SetupFilterableTemplateInfoFromTemplateInfo(IEnumerable<ITemplateInfo> templateList)
        {
            Dictionary<string, HashSet<string>> shortNamesByGroup = new Dictionary<string, HashSet<string>>();

            // get the short names lists for the groups
            foreach (ITemplateInfo template in templateList)
            {
                string effectiveGroupIdentity = !string.IsNullOrEmpty(template.GroupIdentity)
                    ? template.GroupIdentity
                    : template.Identity;

                if (!shortNamesByGroup.TryGetValue(effectiveGroupIdentity, out HashSet<string> shortNames))
                {
                    shortNames = new HashSet<string>();
                    shortNamesByGroup[effectiveGroupIdentity] = shortNames;
                }

                if (template is IShortNameList templateWithShortNameList)
                {
                    shortNames.UnionWith(templateWithShortNameList.ShortNameList);
                }
                else
                {
                    shortNames.Add(template.ShortName);
                }
            }

            // create the FilterableTemplateInfo with the group short names
            List<FilterableTemplateInfo> filterableTemplateList = new List<FilterableTemplateInfo>();

            foreach (ITemplateInfo template in templateList)
            {
                string effectiveGroupIdentity = !string.IsNullOrEmpty(template.GroupIdentity)
                    ? template.GroupIdentity
                    : template.Identity;

                FilterableTemplateInfo filterableTemplate = FilterableTemplateInfo.FromITemplateInfo(template);
                filterableTemplate.GroupShortNameList = shortNamesByGroup[effectiveGroupIdentity].ToList();
                filterableTemplateList.Add(filterableTemplate);
            }

            return filterableTemplateList;
        }

        /// <summary>
        /// Adds match dispositions to the templates based on matches between the default language and the language defined in template.
        /// </summary>
        /// <param name="listToFilter">the templates to match.</param>
        /// <param name="language">default language.</param>
        private static void AddDefaultLanguageMatchingToTemplates(IReadOnlyCollection<ITemplateMatchInfo> listToFilter, string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                return;
            }

            foreach (ITemplateMatchInfo template in listToFilter)
            {
                MatchKind matchKind;

                string templateLanguage = template.Info.GetLanguage();
                // only add default language disposition when there is a language specified for the template.
                if (string.IsNullOrWhiteSpace(templateLanguage))
                {
                    continue;
                }

                if (templateLanguage.Equals(language, StringComparison.OrdinalIgnoreCase))
                {
                    return new MatchInfo(DefaultLanguageMatchParameterName, defaultLanguage, MatchKind.Exact);
                }
            }
        }

        /// <summary>
        /// Adds match dispositions to the templates based on matches between the input parameters and the template specific parameters.
        /// </summary>
        /// <param name="templatesToFilter">the templates to match.</param>
        /// <param name="hostDataLoader"></param>
        /// <param name="commandInput">the command input used in CLI.</param>
        /// <exception cref="CommandParserException">when parsing of command fails.</exception>
        private static IReadOnlyList<ITemplateMatchInfo> AddParameterMatchingToTemplates(IEnumerable<ITemplateMatchInfo> templatesToFilter, IHostSpecificDataLoader hostDataLoader, INewCommandInput commandInput)
        {
            List<ITemplateMatchInfo> processedTemplates = new List<ITemplateMatchInfo>();
            foreach (ITemplateMatchInfo template in templatesToFilter)
            {
                try
                {
                    ParseTemplateArgs(template.Info, hostDataLoader, commandInput);

                    // parameters are already parsed. But choice values aren't checked
                    foreach (KeyValuePair<string, string> matchedParamInfo in commandInput.InputTemplateParams)
                    {
                        string paramName = matchedParamInfo.Key;
                        string paramValue = matchedParamInfo.Value;
                        MatchKind matchStatus = MatchKind.Mismatch;

                        if (template.Info.Tags.TryGetValue(paramName, out ICacheTag paramDetails))
                        {
                            if (string.IsNullOrEmpty(paramValue)
                                && paramDetails is IAllowDefaultIfOptionWithoutValue paramDetailsWithNoValueDefault
                                && !string.IsNullOrEmpty(paramDetailsWithNoValueDefault.DefaultIfOptionWithoutValue))
                            {
                                // The user provided the parameter switch on the command line, without a value.
                                // In this case, the DefaultIfOptionWithoutValue is the effective value.
                                paramValue = paramDetailsWithNoValueDefault.DefaultIfOptionWithoutValue;
                            }

                            if (string.IsNullOrEmpty(paramValue))
                            {
                                matchStatus = MatchKind.InvalidParameterValue;
                            }
                            else if (paramDetails.Choices.ContainsKey(paramValue))
                            {
                                matchStatus = MatchKind.Exact;
                            }
                            else
                            {
                                int startsWithCount = paramDetails.Choices.Count(x => x.Key.StartsWith(paramValue, StringComparison.OrdinalIgnoreCase));
                                if (startsWithCount == 1)
                                {
#pragma warning disable CS0618 // Type or member is obsolete
                                    matchStatus = MatchKind.SingleStartsWith;
#pragma warning restore CS0618 // Type or member is obsolete
                                }
                                else if (startsWithCount > 1)
                                {
#pragma warning disable CS0618 // Type or member is obsolete
                                    matchStatus = MatchKind.AmbiguousParameterValue;
#pragma warning restore CS0618 // Type or member is obsolete
                                }
                                else
                                {
                                    matchStatus = MatchKind.InvalidParameterValue;
                                }
                            }
                        }
                        else if (template.Info.CacheParameters.ContainsKey(paramName))
                        {
                            matchStatus = MatchKind.Exact;
                        }
                        else
                        {
                            matchStatus = MatchKind.InvalidParameterName;
                        }

                        template.AddDisposition(
                            new MatchInfo(
                            paramName,
                            paramValue,
                            matchStatus,
                            commandInput.TemplateParamInputFormat(paramName)));
                    }

                    foreach (string unmatchedParamName in commandInput.RemainingParameters.Keys.Where(x => !x.Contains(':'))) // filter debugging parameter
                    {
                        if (commandInput.TryGetCanonicalNameForVariant(unmatchedParamName, out string canonical))
                        {
                            // the name is a known template parameter, it must have not parsed due to an invalid value
                            // Note (scp 2017-02-27): This probably can't happen, the parameter parsing doesn't check the choice values.
                            template.AddDisposition(
                                new MatchInfo(
                                    canonical,
                                    null,
                                    MatchKind.InvalidParameterName,
                                    commandInput.TemplateParamInputFormat(unmatchedParamName)));
                        }
                        else
                        {
                            // the name is not known
                            // TODO: reconsider storing the canonical in this situation. It's not really a canonical since the parameter is unknown.
                            template.AddDisposition(
                                new MatchInfo(
                                    unmatchedParamName,
                                    null,
                                    MatchKind.InvalidParameterName,
                                    unmatchedParamName));
                        }
                    }
                    processedTemplates.Add(template);
                }
                catch (CommandParserException ex)
                {
                    Reporter.Error.WriteLine(
                        string.Format(
                            LocalizableStrings.TemplateResolver_Warning_FailedToReparseTemplate,
                            $"{template.Info.Identity} ({template.Info.ShortName})"));
                    Reporter.Verbose.WriteLine(string.Format(LocalizableStrings.Generic_Details, ex.ToString()));
                }
            }
            return processedTemplates;
        }
    }
}
