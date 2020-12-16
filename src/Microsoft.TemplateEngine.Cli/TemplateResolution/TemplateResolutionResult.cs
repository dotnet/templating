// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli.TemplateResolution
{
    /// <summary>
    /// The class represents the template resolution result for template instantiation.
    /// The resolution result is the single template to execute
    /// </summary>
    public class TemplateResolutionResult
    {
        public TemplateResolutionResult(string templateName, string userInputLanguage, IReadOnlyCollection<ITemplateMatchInfo> coreMatchedTemplates)
        {
            _templateName = templateName;
            _hasUserInputLanguage = !string.IsNullOrEmpty(userInputLanguage);
            _coreMatchedTemplates = coreMatchedTemplates;
        }

        private readonly string _templateName;
        private readonly bool _hasUserInputLanguage;

        private readonly IReadOnlyCollection<ITemplateMatchInfo> _coreMatchedTemplates;

        // If a single template group can be resolved, return it.
        // If the user input a language, default language results are not considered.
        // ignoreDefaultLanguageFiltering = true will also cause default language filtering to be ignored. Be careful when using this option.
        public bool TryGetUnambiguousTemplateGroupToUse(out IReadOnlyList<ITemplateMatchInfo> unambiguousTemplateGroup, bool ignoreDefaultLanguageFiltering = false)
        {
            if (_coreMatchedTemplates.Count == 0)
            {
                unambiguousTemplateGroup = null;
                return false;
            }

            if (_coreMatchedTemplates.Count == 1)
            {
                unambiguousTemplateGroup = new List<ITemplateMatchInfo>(_coreMatchedTemplates);
                return true;
            }

            // maybe: only use default language if we're trying to invoke
            if (!_hasUserInputLanguage && !ignoreDefaultLanguageFiltering)
            {
                // only consider default language match dispositions if the user did not specify a language.
                List<ITemplateMatchInfo> defaultLanguageMatchedTemplates = _coreMatchedTemplates.Where(x =>
                                                                x.DispositionOfDefaults.Any(y => y.Location == MatchLocation.DefaultLanguage && y.Kind == MatchKind.Exact)
                                                                && !x.MatchDisposition.Any(z => z.Location == MatchLocation.Context && z.Kind == MatchKind.Mismatch))
                                                                            .ToList();

                if (TemplateResolver.AreAllTemplatesSameGroupIdentity(defaultLanguageMatchedTemplates))
                {
                    if (defaultLanguageMatchedTemplates.Any(x => !x.HasParameterMismatch() && !x.HasContextMismatch()))
                    {
                        unambiguousTemplateGroup = defaultLanguageMatchedTemplates.Where(x => !x.HasParameterMismatch() && !x.HasContextMismatch()).ToList();
                        return true;
                    }
                    else
                    {
                        unambiguousTemplateGroup = defaultLanguageMatchedTemplates;
                        return true;
                    }
                }
            }

            List<ITemplateMatchInfo> paramFiltered = _coreMatchedTemplates.Where(x => !x.HasParameterMismatch() && !x.HasContextMismatch()).ToList();
            if (TemplateResolver.AreAllTemplatesSameGroupIdentity(paramFiltered))
            {
                unambiguousTemplateGroup = paramFiltered;
                return true;
            }

            if (TemplateResolver.AreAllTemplatesSameGroupIdentity(_coreMatchedTemplates))
            {
                unambiguousTemplateGroup = new List<ITemplateMatchInfo>(_coreMatchedTemplates);
                return true;
            }

            unambiguousTemplateGroup = null;
            return false;
        }

        public enum Status
        {
            NotEvaluated,
            NoMatch,
            SingleMatch,
            AmbiguousChoice,
            AmbiguousPrecedence
        }

        public bool TryGetSingularInvokableMatch(out ITemplateMatchInfo template, out Status resultStatus)
        {
            IReadOnlyList<ITemplateMatchInfo> invokableMatches = _coreMatchedTemplates.Where(x => x.IsInvokableMatch()).ToList();
            IReadOnlyList<ITemplateMatchInfo> languageFilteredInvokableMatches;

            if (_hasUserInputLanguage)
            {
                languageFilteredInvokableMatches = invokableMatches;
            }
            else
            {
                // check for templates with the default language
                languageFilteredInvokableMatches = invokableMatches.Where(x => x.DispositionOfDefaults.Any(y => y.Location == MatchLocation.DefaultLanguage && y.Kind == MatchKind.Exact)).ToList();

                // no candidate templates matched the default language, continue with the original candidates.
                if (languageFilteredInvokableMatches.Count == 0)
                {
                    languageFilteredInvokableMatches = invokableMatches;
                }
            }

            if (languageFilteredInvokableMatches.Count == 1)
            {
                template = languageFilteredInvokableMatches[0];
                resultStatus = Status.SingleMatch;
                return true;
            }

            // if multiple templates in the group have single starts with matches on the same parameter, it's ambiguous.
            // For the case where one template has single starts with, and another has ambiguous - on the same param:
            //      The one with single starts with is chosen as invokable because if the template with an ambiguous match
            //      was not installed, the one with the singluar invokable would be chosen.
            HashSet<string> singleStartsWithParamNames = new HashSet<string>();
            foreach (ITemplateMatchInfo checkTemplate in languageFilteredInvokableMatches)
            {
                IList<string> singleStartParamNames = checkTemplate.MatchDisposition.Where(x => x.Location == MatchLocation.OtherParameter && x.Kind == MatchKind.SingleStartsWith).Select(x => x.InputParameterName).ToList();
                foreach (string paramName in singleStartParamNames)
                {
                    if (!singleStartsWithParamNames.Add(paramName))
                    {
                        template = null;
                        resultStatus = Status.AmbiguousChoice;
                        return false;
                    }
                }
            }

            ITemplateMatchInfo highestInGroupIfSingleGroup = TemplateResolver.FindHighestPrecedenceTemplateIfAllSameGroupIdentity(languageFilteredInvokableMatches, out bool ambiguousGroupIdResult);

            if (highestInGroupIfSingleGroup != null)
            {
                template = highestInGroupIfSingleGroup;
                resultStatus = Status.SingleMatch;
                return true;
            }
            else if (ambiguousGroupIdResult)
            {
                template = null;
                resultStatus = Status.AmbiguousPrecedence;
                return false;
            }

            template = null;
            resultStatus = Status.NoMatch;
            return false;
        }
    }
}
