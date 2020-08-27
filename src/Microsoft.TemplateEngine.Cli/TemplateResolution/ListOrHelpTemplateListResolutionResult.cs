// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TemplateEngine.Edge.Template;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public bool HasUnambigiousTemplateToUse
        {
            get
            {
                return ExactMatchedTemplates.Count == 1;
            }
        }


        private IReadOnlyList<ITemplateMatchInfo> GetCoreMatchedTemplatesWithDisposition(Func<ITemplateMatchInfo, bool> filter)
        {
            return _coreMatchedTemplates.Where(filter).ToList();
        }

    }
}
