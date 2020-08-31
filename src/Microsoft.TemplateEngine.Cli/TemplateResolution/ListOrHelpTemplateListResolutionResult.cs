// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TemplateEngine.Edge.Template;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Utils;
using System.Collections.ObjectModel;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli.TemplateResolution
{
    /// <summary>
    /// Class represents the template list resolution result to be used for listing or displaying help purposes
    /// </summary>
    public class ListOrHelpTemplateListResolutionResult
    {
        public ListOrHelpTemplateListResolutionResult(string templateName, IReadOnlyCollection<ITemplateMatchInfo> coreMatchedTemplates)
        {
            _templateName = templateName;
            _coreMatchedTemplates = coreMatchedTemplates;
        }

        private readonly string _templateName;
        private readonly IReadOnlyCollection<ITemplateMatchInfo> _coreMatchedTemplates;
        private IReadOnlyCollection<ITemplateMatchInfo> _exactMatchedTemplates;
        private IReadOnlyCollection<ITemplateMatchInfo> _partiallyMatchedTemplates;

        /// <summary>
        /// Returns list of exact or partially matched templates by name and exact match by language, filter, baseline (if specified in command paramaters)
        /// </summary>
        public IReadOnlyCollection<ITemplateMatchInfo> ExactMatchedTemplates
        {
            get
            {
                if (_exactMatchedTemplates == null)
                {
                    _exactMatchedTemplates = GetCoreMatchedTemplatesWithDisposition(x => x.IsMatch);
                }
                return _exactMatchedTemplates;
            }
        }

        /// <summary>
        /// Returns list of exact or partially matched templates by name and exact match by language, filter, baseline (if specified in command paramaters) grouped by group identity
        /// </summary>
        public IReadOnlyCollection<IGrouping<string, ITemplateMatchInfo>> ExactMatchedTemplatesGrouped
        {
            get
            {
                return ExactMatchedTemplates.GroupBy(x => x.Info.GroupIdentity, x => !string.IsNullOrEmpty(x.Info.GroupIdentity)).ToList();
            }
        }

        /// <summary>
        /// Returns list of exact or partially matched templates by name and mismatch in any of the following: language, filter, baseline (if specified in command paramaters)
        /// </summary>
        public IReadOnlyCollection<ITemplateMatchInfo> PartiallyMatchedTemplates
        {
            get
            {
                if (_partiallyMatchedTemplates == null)
                {
                    _partiallyMatchedTemplates = GetCoreMatchedTemplatesWithDisposition(t => t.HasNameMatchOrPartialMatch() && t.HasAnyMismatch());
                }
                return _partiallyMatchedTemplates;
            }
        }

        /// <summary>
        ///  Returns list of exact or partially matched templates by name and mismatch in any of the following: language, filter, baseline (if specified in command paramaters); grouped by group identity
        /// </summary>
        public IReadOnlyCollection<IGrouping<string, ITemplateMatchInfo>> PartiallyMatchedTemplatesGrouped
        {
            get
            {
                return PartiallyMatchedTemplates.GroupBy(x => x.Info.GroupIdentity, x => !string.IsNullOrEmpty(x.Info.GroupIdentity)).ToList();
            }
        }

        public bool HasUnambigiousTemplatesForDefaultLanguage
        {
            get
            {
                if (!HasUnambigiousTemplateGroup)
                {
                    return false;
                }
                return UnambigiousTemplateGroup.Any(t => t.DispositionOfDefaults.Any(y => y.Location == MatchLocation.DefaultLanguage && y.Kind == MatchKind.Exact));
            }
        }

        public IReadOnlyCollection<ITemplateMatchInfo> UnambigiousTemplatesForDefaultLanguage
        {
            get
            {
                return UnambigiousTemplateGroup.Where(t => t.DispositionOfDefaults.Any(y => y.Location == MatchLocation.DefaultLanguage && y.Kind == MatchKind.Exact)).ToList();
            }
        }

        /// <summary>
        /// Returns true when at least one template exactly or partially matched templates by name and exactly matched language, filter, baseline (if specified in command paramaters)
        /// </summary>
        public bool HasExactMatches
        {
            get
            {
                return ExactMatchedTemplates.Any();
            }
        }

        /// <summary>
        /// Returns true when at least one template exactly or partially matched templates by name but has mismatch in any of the following: language, filter, baseline (if specified in command paramaters)
        /// </summary>
        public bool HasPartialMatches
        {
            get
            {
                return PartiallyMatchedTemplates.Any();
            }
        }

        /// <summary>
        /// Returns true when at least one template has mismatch in language
        /// </summary>
        public bool HasLanguageMismatch
        {
            get
            {
                return PartiallyMatchedTemplates.Any(t => t.HasLanguageMismatch());
            }
        }

        /// <summary>
        /// Returns true when at least one template has mismatch in context (type)
        /// </summary>
        public bool HasContextMismatch
        {
            get
            {
                return PartiallyMatchedTemplates.Any(t => t.HasContextMismatch());
            }
        }

        /// <summary>
        /// Returns true when at least one template has mismatch in baseline
        /// </summary>
        public bool HasBaselineMismatch
        {
            get
            {
                return PartiallyMatchedTemplates.Any(t => t.HasBaselineMismatch());
            }
        }

        /// <summary>
        /// Returns true when one and only one template has exact match
        /// </summary>
        public bool HasUnambigiousTemplateGroup
        {
            get
            {
                return ExactMatchedTemplatesGrouped.Count == 1;
            }
        }

        /// <summary>
        /// Returns list of templates for unambigious template group, otherwise empty list
        /// </summary>
        public IReadOnlyCollection<ITemplateMatchInfo> UnambigiousTemplateGroup
        {
            get
            {
                return HasUnambigiousTemplateGroup ? ExactMatchedTemplates : new List <ITemplateMatchInfo>();
            }
        }

        /// <summary>
        /// Returns true if all the templates in unambigious group have templates in same language
        /// </summary>
        public bool AllTemplatesInUnambigiousTemplateGroupAreSameLanguage
        {
            get
            {
                if (UnambigiousTemplateGroup.Count == 0)
                {
                    return false;
                }

                if (UnambigiousTemplateGroup.Count == 1)
                {
                    return true;
                }
                    
                HashSet<string> languagesFound = new HashSet<string>();
                foreach (ITemplateMatchInfo template in UnambigiousTemplateGroup)
                {
                    string language;

                    if (template.Info.Tags != null && template.Info.Tags.TryGetValue("language", out ICacheTag languageTag))
                    {
                        language = languageTag.ChoicesAndDescriptions.Keys.FirstOrDefault();
                    }
                    else
                    {
                        language = string.Empty;
                    }

                    if (!string.IsNullOrEmpty(language))
                    {
                        languagesFound.Add(language);
                    }

                    if (languagesFound.Count > 1)
                    {
                        return false;
                    }
                }
                return true;
            }
        }


        private IReadOnlyCollection<ITemplateMatchInfo> GetCoreMatchedTemplatesWithDisposition(Func<ITemplateMatchInfo, bool> filter)
        {
            return _coreMatchedTemplates.Where(filter).ToList();
        }

    }
}
