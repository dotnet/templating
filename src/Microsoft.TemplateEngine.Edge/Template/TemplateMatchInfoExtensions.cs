// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.TemplateEngine.Edge.Template
{
    /// <summary>
    /// Set of useful extensions when working with <see cref="ITemplateMatchInfo"/>.
    /// </summary>
    public static class TemplateMatchInfoExtensions
    {
        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Exact" /> or <see cref="MatchKind.Partial" /> match on <see cref="MatchInfo.DefaultParameter.Name"/>.
        /// </summary>
        public static bool HasNameMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Name && (x.Kind == MatchKind.Exact || x.Kind == MatchKind.Partial));
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Exact" /> match on <see cref="MatchInfo.DefaultParameter.Name"/>.
        /// </summary>
        public static bool HasNameExactMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Name && x.Kind == MatchKind.Exact);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Partial" /> match on <see cref="MatchInfo.DefaultParameter.Name"/>.
        /// </summary>
        public static bool HasNamePartialMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Name && x.Kind == MatchKind.Partial);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Mismatch" /> on <see cref="MatchInfo.DefaultParameter.Name"/>.
        /// </summary>
        public static bool HasNameMismatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Name && x.Kind == MatchKind.Mismatch);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Exact" /> or <see cref="MatchKind.Partial" /> match on <see cref="MatchInfo.DefaultParameter.ShortName"/>.
        /// </summary>
        public static bool HasShortNameMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.ShortName && (x.Kind == MatchKind.Exact || x.Kind == MatchKind.Partial));
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Exact" /> match on <see cref="MatchInfo.DefaultParameter.ShortName"/>.
        /// </summary>
        public static bool HasShortNameExactMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.ShortName && x.Kind == MatchKind.Exact);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Partial" /> match on <see cref="MatchInfo.DefaultParameter.ShortName"/>.
        /// </summary>
        public static bool HasShortNamePartialMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.ShortName && x.Kind == MatchKind.Partial);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Mismatch" /> on <see cref="MatchInfo.DefaultParameter.ShortName"/>.
        /// </summary>
        public static bool HasShortNameMismatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.ShortName && x.Kind == MatchKind.Mismatch);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Exact" /> match on <see cref="MatchInfo.DefaultParameter.Type"/>.
        /// </summary>
        public static bool HasTypeMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Type && x.Kind == MatchKind.Exact);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Mismatch" /> on <see cref="MatchInfo.DefaultParameter.Type"/>.
        /// </summary>
        public static bool HasTypeMismatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Type && x.Kind == MatchKind.Mismatch);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Exact" /> match on <see cref="MatchInfo.DefaultParameter.Classification"/>.
        /// </summary>
        public static bool HasClassificationMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Classification && x.Kind == MatchKind.Exact);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Mismatch" /> on <see cref="MatchInfo.DefaultParameter.Classification"/>.
        /// </summary>
        public static bool HasClassificationMismatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Classification && x.Kind == MatchKind.Mismatch);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Exact" /> match on <see cref="MatchInfo.DefaultParameter.Language"/>.
        /// </summary>
        public static bool HasLanguageMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Language && x.Kind == MatchKind.Exact);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Mismatch" /> on <see cref="MatchInfo.DefaultParameter.Language"/>.
        /// </summary>
        public static bool HasLanguageMismatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Language && x.Kind == MatchKind.Mismatch);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Exact" /> match on <see cref="MatchInfo.DefaultParameter.Baseline"/>.
        /// </summary>
        public static bool HasBaselineMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Baseline && x.Kind == MatchKind.Exact);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Mismatch" /> on <see cref="MatchInfo.DefaultParameter.Baseline"/>.
        /// </summary>
        public static bool HasBaselineMismatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Baseline && x.Kind == MatchKind.Mismatch);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Exact" /> or <see cref="MatchKind.Partial" /> match on <see cref="MatchInfo.DefaultParameter.Author"/>.
        /// </summary>
        public static bool HasAuthorMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Author && (x.Kind == MatchKind.Exact || x.Kind == MatchKind.Partial));
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Exact" /> match on <see cref="MatchInfo.DefaultParameter.Author"/>.
        /// </summary>
        public static bool HasAuthorExactMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Author && x.Kind == MatchKind.Exact);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Partial" /> match on <see cref="MatchInfo.DefaultParameter.Author"/>.
        /// </summary>
        public static bool HasAuthorPartialMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Author && x.Kind == MatchKind.Partial);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.Mismatch" /> on <see cref="MatchInfo.DefaultParameter.Author"/>.
        /// </summary>
        public static bool HasAuthorMismatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == MatchInfo.DefaultParameter.Author && x.Kind == MatchKind.Mismatch);
        }

        /// <summary>
        /// Returns true when <paramref name="templateMatchInfo"/> has <see cref="MatchKind.InvalidParameterName" /> on any disposition.
        /// </summary>
        public static bool HasInvalidParameterName(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.Kind == MatchKind.InvalidParameterName);
        }

        /// <summary>
        /// Returns the list of <see cref="MatchInfo.ParameterName"/> that have <see cref="MatchKind.InvalidParameterName" />.
        /// </summary>
        public static IReadOnlyList<string> GetInvalidParameterNames(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Where(x => x.Kind == MatchKind.InvalidParameterName)
                                                   .Select(x => x.ParameterName).ToList();
        }
    }
}
