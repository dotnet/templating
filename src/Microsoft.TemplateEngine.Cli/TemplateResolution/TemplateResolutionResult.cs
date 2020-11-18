// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Cli.TemplateResolution
{
    /// <summary>
    /// Class represents the template resolution result for template instantiation based on command input.
    /// Before template is resolved all installed templates are grouped by template group ID. Templates in single group:<br/>
    /// - should have different template identity <br/>
    /// - same short name (however different short names are also supported) <br/>
    /// - the templates may have different languages and types <br/>
    /// - the templates should have different precedence value in case same language is used <br/>
    /// - the templates in the group may have different parameters and different choices for parameter symbols defined<br/>
    /// Template resolution is done in several steps:<br/>
    /// 1) the list of matched templates is defined based on command input (template name and filters used)<br/>
    /// 2) the unambiguous template group to use is defined. In case of ambiguity, if the user didn't specify language to use, the groups with the templates defined in default language are preferred. In case unambiguous template group cannot be resolved, the template to instatiate cannot be resolved as well.<br/>
    /// 3) the template to invoke inside the group is defined:<br/>
    /// -- the template that matches template specific options in command input is preferred<br/>
    /// -- in case there are multiple templates, the one with highest precedence is selected<br/>
    /// -- in case there are multiple templates with same precendence and user didn't specify the language to use, the default language template is preferred.<br/>
    /// -- Note that the template will not be resolved in case the value specified for choice parameter is not exact and there is an ambiguity between template to select:<br/>
    /// --- in case at least one template in the group has more than 1 choice value which starts with specified value in the command<br/>
    /// --- in case at least two templates in the group have 1 choice value which starts with specified value in the command<br/>
    /// </summary>
    public class TemplateResolutionResult
    {
        private readonly IReadOnlyCollection<ITemplateMatchInfo> _coreMatchedTemplates;

        private readonly bool _hasUserInputLanguage;

        private SingleInvokableMatchStatus _singularInvokableMatchStatus = SingleInvokableMatchStatus.NotEvaluated;

        private IReadOnlyCollection<TemplateGroup> _templateGroups;

        private ITemplateMatchInfo _templateToInvoke = null;

        private TemplateGroup _unambiguousTemplateGroup = null;

        private UnambiguousTemplateGroupStatus _unambigiousTemplateGroupStatus = UnambiguousTemplateGroupStatus.NotEvaluated;

        public TemplateResolutionResult(string userInputLanguage, IReadOnlyCollection<ITemplateMatchInfo> coreMatchedTemplates)
        {
            _hasUserInputLanguage = !string.IsNullOrEmpty(userInputLanguage);
            _coreMatchedTemplates = coreMatchedTemplates;
        }

        /// <summary>
        /// Enum defines possible statuses for resolving template to invoke:<br />
        /// - <see cref="SingleInvokableMatchStatus.NotEvaluated"/> - the status is not evaluated yet.<br/>
        /// - <see cref="SingleInvokableMatchStatus.SingleMatch"/> - single template group and single template to use in the group is evaluated. <br/>
        /// - <see cref="SingleInvokableMatchStatus.NoMatch"/> - no matched template groups were resolved<br/>
        /// - <see cref="SingleInvokableMatchStatus.AmbiguousTemplateGroupChoice"/> - multiple template groups were resolved; not possible to determing the group to use<br/>
        /// - <see cref="SingleInvokableMatchStatus.AmbiguousParameterValueChoice"/> - single template group was resolved, but it is not possible to resolve choice parameter value to use. <br/>
        /// - <see cref="SingleInvokableMatchStatus.AmbiguousTemplateChoice"/> - single template group was resolved, but there is an ambiguous choice for template inside the group. Ususlly means that the installed templates are conflicting and the confict should be resolved by uninistalling some of templates.<br/>
        /// - <see cref="SingleInvokableMatchStatus.InvalidParameter"/> - single template group was resolved, but parameters or choice parameter values provided are invalid for all templates in the group.<br/>
        /// </summary>
        public enum SingleInvokableMatchStatus
        {
            NotEvaluated,
            NoMatch,
            SingleMatch,
            AmbiguousTemplateGroupChoice,
            AmbiguousParameterValueChoice,
            AmbiguousTemplateChoice,
            InvalidParameter
        }

        internal enum UnambiguousTemplateGroupStatus
        {
            NotEvaluated,
            NoMatch,
            SingleMatch,
            Ambiguous
        }


        /// <summary>
        /// Returns status of template resolution: <br />
        /// - <see cref="SingleInvokableMatchStatus.SingleMatch"/> - single template group and single template to use in the group is evaluated. In this case <see cref="TemplateToInvoke"/> contains the template resolved.<br/>
        /// - <see cref="SingleInvokableMatchStatus.NoMatch"/> - no matched template groups were resolved. <br/>
        /// - <see cref="SingleInvokableMatchStatus.AmbiguousTemplateGroupChoice"/> - multiple template groups were resolved; not possible to determing the group to use<br/>
        /// - <see cref="SingleInvokableMatchStatus.AmbiguousParameterValueChoice"/> - single template group was resolved, but it is not possible to resolve choice parameter value to use. <br/>
        /// - <see cref="SingleInvokableMatchStatus.AmbiguousTemplateChoice"/> - single template group was resolved, but there is an ambiguous choice for template inside the group. Ususlly means that the installed templates are conflicting and the confict should be resolved by uninistalling some of templates.<br/>
        /// - <see cref="SingleInvokableMatchStatus.InvalidParameter"/> - single template group was resolved, but parameters or choice parameter values provided are invalid for all templates in the group.<br/>
        /// </summary>
        internal SingleInvokableMatchStatus Status
        {
            get
            {
                if (_singularInvokableMatchStatus == SingleInvokableMatchStatus.NotEvaluated)
                {
                    EvaluateTemplateToInvoke();
                }
                return _singularInvokableMatchStatus;
            }
        }

        /// <summary>
        /// Returns the template to invoke or <see cref="null"/> if the template to invoke cannot be determined.
        /// Has value only when <see cref="Status" /> is <see cref="SingleInvokableMatchStatus.SingleMatch"/>
        /// </summary>
        internal ITemplateMatchInfo TemplateToInvoke
        {
            get
            {
                if (_singularInvokableMatchStatus == SingleInvokableMatchStatus.NotEvaluated)
                {
                    EvaluateTemplateToInvoke();
                }
                return _templateToInvoke;
            }
        }

        /// <summary>
        /// Returns template groups that matches command input (template specific options are not considered in the match)
        /// </summary>
        internal IReadOnlyCollection<TemplateGroup> TemplateGroups
        {
            get
            {
                if (_templateGroups == null)
                {
                    _templateGroups = _coreMatchedTemplates
                        .GroupBy(x => x.Info.GroupIdentity, x => !string.IsNullOrEmpty(x.Info.GroupIdentity), StringComparer.OrdinalIgnoreCase)
                        .Select(group => new TemplateGroup(group.Key, group.ToList()))
                        .ToList();
                }
                return _templateGroups;
            }
        }

        /// <summary>
        /// Returns status of unambiguous template group resolution
        /// - <see cref="UnambiguousTemplateGroupStatus.SingleMatch"/> - single template group is evaluated. In this case <see cref="UnambiguousTemplateGroup"/> contains the template group resolved.<br/>
        /// - <see cref="UnambiguousTemplateGroupStatus.NoMatch"/> - no matched template groups were. <br/>
        /// - <see cref="UnambiguousTemplateGroupStatus.AmbiguousTemplateGroupChoice"/> - multiple template groups were resolved; not possible to determing the group to use<br/>
        /// </summary>
        internal UnambiguousTemplateGroupStatus UnambigiousTemplateGroupCheckStatus
        {
            get
            {
                if (_unambigiousTemplateGroupStatus == UnambiguousTemplateGroupStatus.NotEvaluated)
                {
                    EvaluateUnambiguousTemplateGroup();
                }
                return _unambigiousTemplateGroupStatus;
            }
        }

        /// <summary>
        /// Returns unambiguous template group resolved; <see cref="null"/> if group cannot be resolved based on command input
        /// Has value only when <see cref="UnambigiousTemplateGroupCheckStatus" /> is <see cref="UnambigiousTemplateGroupCheckStatus.SingleMatch"/>
        /// </summary>
        internal TemplateGroup UnambiguousTemplateGroup
        {
            get
            {
                if (_unambigiousTemplateGroupStatus == UnambiguousTemplateGroupStatus.NotEvaluated)
                {
                    EvaluateUnambiguousTemplateGroup();
                }
                return _unambiguousTemplateGroup;
            }
        }

        /// <summary>
        /// The method evaluates whether the template resolution result contains single template to invoke.
        /// The methos is deprecated: use <see cref="Status"/> to get the template resolution status and <see cref="TemplateToInvoke"/> to get the template to invoke.
        /// </summary>
        /// <param name="template">the single template to invoke; <see cref="null"/> if the template cannot be resolved</param>
        /// <param name="resultStatus">contains <seealso cref="Status"/></param>
        /// <returns>
        /// <see cref="true"/> when the template to invoke is resolved<br/>
        /// <see cref="false"/> otherwise<br/>
        /// </returns>
        public bool TryGetSingularInvokableMatch(out ITemplateMatchInfo template, out SingleInvokableMatchStatus resultStatus)
        {
            template = TemplateToInvoke;
            resultStatus = Status;
            return Status == SingleInvokableMatchStatus.SingleMatch;
        }


        /// <summary>
        /// The method evaluates whether the template resolution result contains unambiguous template group to use.
        /// The methos is deprecated: use <see cref="UnambigiousTemplateGroupCheckStatus"/> to get the template resolution status and <see cref="UnambiguousTemplateGroup"/> to get the template to invoke.
        /// </summary>
        /// <param name="unambiguousTemplateGroup">the unambiguous template group to use; <see cref="null"/> if the unambiguous template group cannot be resolved</param>
        /// <returns>
        /// <see cref="true"/> when the unambiguous template group to use is resolved<br/>
        /// <see cref="false"/> otherwise<br/>
        /// </returns>
        internal bool TryGetUnambiguousTemplateGroupToUse(out IReadOnlyCollection<ITemplateMatchInfo> unambiguousTemplateGroup)
        {
            unambiguousTemplateGroup = UnambiguousTemplateGroup?.Templates;
            return unambiguousTemplateGroup != null;
        }

        private void EvaluateTemplateToInvoke()
        {
            //if no template groups were matched - no match
            if (UnambigiousTemplateGroupCheckStatus == UnambiguousTemplateGroupStatus.NoMatch)
            {
                _singularInvokableMatchStatus = SingleInvokableMatchStatus.NoMatch;
                return;
            }

            if (UnambigiousTemplateGroupCheckStatus == UnambiguousTemplateGroupStatus.Ambiguous)
            {
                _singularInvokableMatchStatus = SingleInvokableMatchStatus.AmbiguousTemplateGroupChoice;
                return;
            }

            //checking template options match
            //if any template in the group has ambiguous parameter value match - cannot resolve template to instantiate
            if (UnambiguousTemplateGroup.Templates.Any(x => x.HasAmbiguousParameterValueMatch()))
            {
                _singularInvokableMatchStatus = SingleInvokableMatchStatus.AmbiguousParameterValueChoice;
                return;
            }

            //if no templates are invokable there is a problem with parameter name or value - cannot resolve template to instantiate
            if (!UnambiguousTemplateGroup.InvokableTemplates.Any())
            {
                _singularInvokableMatchStatus = SingleInvokableMatchStatus.InvalidParameter;
                return;
            }

            if (UnambiguousTemplateGroup.InvokableTemplates.Count() == 1)
            {
                _templateToInvoke = UnambiguousTemplateGroup.InvokableTemplates.First();
                _singularInvokableMatchStatus = SingleInvokableMatchStatus.SingleMatch;
                return;
            }

            // if multiple templates in the group have single starts with matches on the same parameter, it's ambiguous.
            // For the case where one template has single starts with, and another has ambiguous - on the same param:
            //      The one with single starts with is chosen as invokable because if the template with an ambiguous match
            //      was not installed, the one with the singluar invokable would be chosen.
            if (UnambiguousTemplateGroup.GetAmbiguousSingleStartsWithParameters().Any())
            {
                _singularInvokableMatchStatus = SingleInvokableMatchStatus.AmbiguousParameterValueChoice;
                return;
            }

            if (UnambiguousTemplateGroup.TryGetHighestPrecedenceInvokableTemplate(out _templateToInvoke, !_hasUserInputLanguage))
            {
                _singularInvokableMatchStatus = SingleInvokableMatchStatus.SingleMatch;
                return;
            }

            _singularInvokableMatchStatus = SingleInvokableMatchStatus.AmbiguousTemplateChoice;
        }

        private void EvaluateUnambiguousTemplateGroup()
        {
            if (TemplateGroups.Count == 0)
            {
                _unambigiousTemplateGroupStatus = UnambiguousTemplateGroupStatus.NoMatch;
                return;
            }
            if (TemplateGroups.Count == 1)
            {
                _unambiguousTemplateGroup = TemplateGroups.First();
                _unambigiousTemplateGroupStatus = UnambiguousTemplateGroupStatus.SingleMatch;
                return;
            }
            //TODO: check if it is correct logic
            else if (!_hasUserInputLanguage)
            {
                // only consider default language match dispositions if the user did not specify a language.
                try
                {
                    _unambiguousTemplateGroup = TemplateGroups.SingleOrDefault(group => group.Templates.Any(x => x.HasDefaultLanguageMatch()));
                }
                catch (InvalidOperationException)
                {
                    _unambigiousTemplateGroupStatus = UnambiguousTemplateGroupStatus.Ambiguous;
                    return;
                }
                if (_unambiguousTemplateGroup != null)
                {
                    _unambigiousTemplateGroupStatus = UnambiguousTemplateGroupStatus.SingleMatch;
                    return;
                }
            }
            _unambigiousTemplateGroupStatus = UnambiguousTemplateGroupStatus.Ambiguous;
        }
    }
}
