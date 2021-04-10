// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Edge.Template
{
    /// <summary>
    /// Collection of default filters to be used with <see cref="TemplateListFilter.GetTemplateMatchInfo(System.Collections.Generic.IReadOnlyList{ITemplateInfo}, Func{ITemplateMatchInfo, bool}, Func{ITemplateInfo, MatchInfo?}[])"/>.
    /// </summary>
    public static class WellKnownSearchFilters
    {
        /// <summary>
        /// Filter to be used with <see cref="TemplateListFilter.GetTemplateMatchInfo(System.Collections.Generic.IReadOnlyList{ITemplateInfo}, Func{ITemplateMatchInfo, bool}, Func{ITemplateInfo, MatchInfo?}[])"/>.
        /// Matches <paramref name="name"/> on the following criteria: <br/>
        /// - if <paramref name="name"/> is null or empty, adds match disposition <see cref="MatchInfo.DefaultParameter.Name"/> with <see cref="MatchKind.Partial"/>;<br/>
        /// - if <paramref name="name"/> is equal to <see cref="ITemplateInfo.Name"/> (case insensitive), adds match disposition <see cref="MatchInfo.DefaultParameter.Name"/> with <see cref="MatchKind.Exact"/>;<br/>
        /// - if <paramref name="name"/> is equal to one of the short names in <see cref="ITemplateInfo.ShortNameList"/> (case insensitive), adds match disposition <see cref="MatchInfo.DefaultParameter.ShortName"/> with <see cref="MatchKind.Exact"/>;<br/>
        /// - if <see cref="ITemplateInfo.Name"/> contains <paramref name="name"/> (case insensitive), adds match disposition <see cref="MatchInfo.DefaultParameter.Name"/> with <see cref="MatchKind.Partial"/>;<br/>
        /// - if one of the short names in <see cref="ITemplateInfo.ShortNameList"/> contains <paramref name="name"/> (case insensitive), adds match disposition <see cref="MatchInfo.DefaultParameter.ShortName"/> with <see cref="MatchKind.Partial"/>;<br/>
        /// - adds match disposition <see cref="MatchInfo.DefaultParameter.Name"/> with <see cref="MatchKind.Mismatch"/> otherwise.<br/>
        /// </summary>
        /// <returns> the predicate to be used with <see cref="TemplateListFilter.GetTemplateMatchInfo(System.Collections.Generic.IReadOnlyList{ITemplateInfo}, Func{ITemplateMatchInfo, bool}, Func{ITemplateInfo, MatchInfo?}[])"/> as the filter.</returns>
        public static Func<ITemplateInfo, MatchInfo?> NameFilter(string name)
        {
            return (template) =>
            {
                if (string.IsNullOrEmpty(name))
                {
                    return new MatchInfo(MatchInfo.DefaultParameter.Name, name, MatchKind.Partial);
                }

                int nameIndex = template.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase);

                if (nameIndex == 0 && string.Equals(template.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return new MatchInfo(MatchInfo.DefaultParameter.Name, name, MatchKind.Exact);
                }

                bool hasShortNamePartialMatch = false;

                if (template is FilterableTemplateInfo filterableTemplate)
                {
                    foreach (string shortName in filterableTemplate.GroupShortNameList)
                    {
                        int shortNameIndex = shortName.IndexOf(name, StringComparison.OrdinalIgnoreCase);

                        if (shortNameIndex == 0 && string.Equals(shortName, name, StringComparison.OrdinalIgnoreCase))
                        {
                            return new MatchInfo(MatchInfo.DefaultParameter.ShortName, name, MatchKind.Exact);
                        }

                        hasShortNamePartialMatch |= shortNameIndex > -1;
                    }
                }
                else
                {
                    int shortNameIndex = template.ShortName.IndexOf(name, StringComparison.OrdinalIgnoreCase);

                    if (shortNameIndex == 0 && string.Equals(template.ShortName, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return new MatchInfo(MatchInfo.DefaultParameter.ShortName, name, MatchKind.Exact);
                    }

                    hasShortNamePartialMatch = shortNameIndex > -1;
                }

                if (nameIndex > -1)
                {
                    return new MatchInfo(MatchInfo.DefaultParameter.Name, name, MatchKind.Partial);
                }

                if (hasShortNamePartialMatch)
                {
                    return new MatchInfo(MatchInfo.DefaultParameter.ShortName, name, MatchKind.Partial);
                }
                return new MatchInfo(MatchInfo.DefaultParameter.Name, name, MatchKind.Mismatch);
            };
        }

        [Obsolete("Use TypeFilter instead")]
        // This being case-insensitive depends on the dictionaries on the cache tags being declared as case-insensitive
        public static Func<ITemplateInfo, MatchInfo?> ContextFilter(string inputContext)
        {
            string? context = inputContext?.ToLowerInvariant();

            return (template) =>
            {
                if (string.IsNullOrEmpty(context))
                {
                    return null;
                }
                if (template.GetTemplateType()?.Equals(context, StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    return new MatchInfo { Location = MatchLocation.Context, Kind = MatchKind.Exact };
                }
                else
                {
                    return new MatchInfo { Location = MatchLocation.Context, Kind = MatchKind.Mismatch };
                }
            };
        }

        /// <summary>
        /// Filter to be used with <see cref="TemplateListFilter.GetTemplateMatchInfo(System.Collections.Generic.IReadOnlyList{ITemplateInfo}, Func{ITemplateMatchInfo, bool}, Func{ITemplateInfo, MatchInfo?}[])"/>.
        /// Matches <paramref name="inputType"/> on the following criteria: <br/>
        /// - if <paramref name="inputType"/> is null or empty, does not add match disposition;<br/>
        /// - if <paramref name="inputType"/> is equal to tag named 'type' from <see cref="ITemplateInfo.Tags"/> (case insensitive), adds match disposition <see cref="MatchInfo.DefaultParameter.Type"/> with <see cref="MatchKind.Exact"/>;<br/>
        /// - adds match disposition <see cref="MatchInfo.DefaultParameter.Type"/> with <see cref="MatchKind.Mismatch"/> otherwise.<br/>
        /// </summary>
        /// <returns> the predicate to be used with <see cref="TemplateListFilter.GetTemplateMatchInfo(System.Collections.Generic.IReadOnlyList{ITemplateInfo}, Func{ITemplateMatchInfo, bool}, Func{ITemplateInfo, MatchInfo?}[])"/> as the filter.</returns>
        public static Func<ITemplateInfo, MatchInfo?> TypeFilter(string? inputType)
        {
            string? type = inputType?.ToLowerInvariant();

            return (template) =>
            {
                if (string.IsNullOrEmpty(type))
                {
                    return null;
                }
                if (template.GetTemplateType()?.Equals(type, StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    return new MatchInfo(MatchInfo.DefaultParameter.Type, type, MatchKind.Exact);
                }
                else
                {
                    return new MatchInfo(MatchInfo.DefaultParameter.Type, type, MatchKind.Mismatch);
                }
            };
        }

        /// <summary>
        /// Filter to be used with <see cref="TemplateListFilter.GetTemplateMatchInfo(System.Collections.Generic.IReadOnlyList{ITemplateInfo}, Func{ITemplateMatchInfo, bool}, Func{ITemplateInfo, MatchInfo?}[])"/>.
        /// Matches <paramref name="classification"/> on the following criteria: <br/>
        /// - if <paramref name="classification"/> is null or empty, does not add match disposition;<br/>
        /// - if <paramref name="classification"/> is equal to any entry from <see cref="ITemplateInfo.Classification"/> (case insensitive), adds match disposition <see cref="MatchInfo.DefaultParameter.Classification"/> with <see cref="MatchKind.Exact"/>;<br/>
        /// - adds match disposition <see cref="MatchInfo.DefaultParameter.Classification"/> with <see cref="MatchKind.Mismatch"/> otherwise.<br/>
        /// </summary>
        /// <returns> the predicate to be used with <see cref="TemplateListFilter.GetTemplateMatchInfo(System.Collections.Generic.IReadOnlyList{ITemplateInfo}, Func{ITemplateMatchInfo, bool}, Func{ITemplateInfo, MatchInfo?}[])"/> as the filter.</returns>
        public static Func<ITemplateInfo, MatchInfo?> ClassificationFilter(string classification)
        {
            return (template) =>
            {
                if (string.IsNullOrWhiteSpace(classification))
                {
                    return null;
                }
                if (template.Classifications?.Contains(classification, StringComparer.OrdinalIgnoreCase) ?? false)
                {
                    return new MatchInfo(MatchInfo.DefaultParameter.Classification, classification, MatchKind.Exact);
                }
                return new MatchInfo(MatchInfo.DefaultParameter.Classification, classification, MatchKind.Mismatch);
            };
        }

        /// <summary>
        /// Filter to be used with <see cref="TemplateListFilter.GetTemplateMatchInfo(System.Collections.Generic.IReadOnlyList{ITemplateInfo}, Func{ITemplateMatchInfo, bool}, Func{ITemplateInfo, MatchInfo?}[])"/>.
        /// Matches <paramref name="language"/> on the following criteria: <br/>
        /// - if <paramref name="language"/> is null or empty, does not add match disposition;<br/>
        /// - if <paramref name="language"/> is equal to tag named 'language' from <see cref="ITemplateInfo.Tags"/> (case insensitive), adds match disposition <see cref="MatchInfo.DefaultParameter.Language"/> with <see cref="MatchKind.Exact"/>;<br/>
        /// - adds match disposition <see cref="MatchInfo.DefaultParameter.Language"/> with <see cref="MatchKind.Mismatch"/> otherwise.<br/>
        /// </summary>
        /// <returns> the predicate to be used with <see cref="TemplateListFilter.GetTemplateMatchInfo(System.Collections.Generic.IReadOnlyList{ITemplateInfo}, Func{ITemplateMatchInfo, bool}, Func{ITemplateInfo, MatchInfo?}[])"/> as the filter.</returns>
        public static Func<ITemplateInfo, MatchInfo?> LanguageFilter(string language)
        {
            return (template) =>
            {
                if (string.IsNullOrEmpty(language))
                {
                    return null;
                }

                if (template.GetLanguage()?.Equals(language, StringComparison.OrdinalIgnoreCase) ?? false)
                {
                    return new MatchInfo(MatchInfo.DefaultParameter.Language, language, MatchKind.Exact);
                }
                else
                {
                    return new MatchInfo(MatchInfo.DefaultParameter.Language, language, MatchKind.Mismatch);
                }
            };
        }

        /// <summary>
        /// Filter to be used with <see cref="TemplateListFilter.GetTemplateMatchInfo(System.Collections.Generic.IReadOnlyList{ITemplateInfo}, Func{ITemplateMatchInfo, bool}, Func{ITemplateInfo, MatchInfo?}[])"/>.
        /// Matches <paramref name="baselineName"/> on the following criteria: <br/>
        /// - if <paramref name="baselineName"/> is null or empty, does not add match disposition;<br/>
        /// - if <paramref name="baselineName"/> is equal to key from <see cref="ITemplateInfo.BaselineInfo"/> (case insensitive), adds match disposition <see cref="MatchInfo.DefaultParameter.Baseline"/> with <see cref="MatchKind.Exact"/>;<br/>
        /// - adds match disposition <see cref="MatchInfo.DefaultParameter.Baseline"/> with <see cref="MatchKind.Mismatch"/> otherwise.<br/>
        /// </summary>
        /// <returns> the predicate to be used with <see cref="TemplateListFilter.GetTemplateMatchInfo(System.Collections.Generic.IReadOnlyList{ITemplateInfo}, Func{ITemplateMatchInfo, bool}, Func{ITemplateInfo, MatchInfo?}[])"/> as the filter.</returns>
        public static Func<ITemplateInfo, MatchInfo?> BaselineFilter(string baselineName)
        {
            return (template) =>
            {
                if (string.IsNullOrEmpty(baselineName))
                {
                    return null;
                }

                if (template.BaselineInfo != null && template.BaselineInfo.ContainsKey(baselineName))
                {
                    return new MatchInfo(MatchInfo.DefaultParameter.Baseline, baselineName, MatchKind.Exact);
                }
                else
                {
                    return new MatchInfo(MatchInfo.DefaultParameter.Baseline, baselineName, MatchKind.Mismatch);
                }
            };
        }

        [Obsolete("Use ClassificationFilter instead")]
        public static Func<ITemplateInfo, MatchInfo?> ClassificationsFilter(string name)
        {
            return (template) =>
            {
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }

                string[] parts = name.Split('/');

                if (template.Classifications != null)
                {
                    bool allParts = true;
                    bool anyParts = false;

                    foreach (string part in parts)
                    {
                        if (!template.Classifications.Contains(part, StringComparer.OrdinalIgnoreCase))
                        {
                            allParts = false;
                        }
                        else
                        {
                            anyParts = true;
                        }
                    }

                    anyParts &= parts.Length == template.Classifications.Count;

                    if (allParts || anyParts)
                    {
                        return new MatchInfo { Location = MatchLocation.Classification, Kind = allParts ? MatchKind.Exact : MatchKind.Partial };
                    }
                }

                return null;
            };
        }

        /// <summary>
        /// Filter to be used with <see cref="TemplateListFilter.GetTemplateMatchInfo(System.Collections.Generic.IReadOnlyList{ITemplateInfo}, Func{ITemplateMatchInfo, bool}, Func{ITemplateInfo, MatchInfo?}[])"/>.
        /// Matches <paramref name="author"/> on the following criteria: <br/>
        /// - if <paramref name="author"/> is null or empty, does not add match disposition;<br/>
        /// - if <see cref="ITemplateInfo.Author"/> is null or empty, adds match disposition <see cref="MatchInfo.DefaultParameter.Author"/> with <see cref="MatchKind.Mismatch"/>;<br/>
        /// - if <paramref name="author"/> is equal to <see cref="ITemplateInfo.Author"/> (case insensitive), adds match disposition <see cref="MatchInfo.DefaultParameter.Author"/> with <see cref="MatchKind.Exact"/>;<br/>
        /// - if <see cref="ITemplateInfo.Author"/> contains <paramref name="author"/> (case insensitive), adds match disposition <see cref="MatchInfo.DefaultParameter.Author"/> with <see cref="MatchKind.Partial"/>;<br/>
        /// - <see cref="MatchInfo.DefaultParameter.Author"/> with <see cref="MatchKind.Mismatch"/> otherwise.<br/>
        /// </summary>
        /// <returns> the predicate to be used with <see cref="TemplateListFilter.GetTemplateMatchInfo(System.Collections.Generic.IReadOnlyList{ITemplateInfo}, Func{ITemplateMatchInfo, bool}, Func{ITemplateInfo, MatchInfo?}[])"/> as the filter.</returns>
        public static Func<ITemplateInfo, MatchInfo?> AuthorFilter(string author)
        {
            return (template) =>
            {
                if (string.IsNullOrWhiteSpace(author))
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(template.Author))
                {
                    return new MatchInfo(MatchInfo.DefaultParameter.Author, author, MatchKind.Mismatch);
                }

                int authorIndex = template.Author.IndexOf(author, StringComparison.OrdinalIgnoreCase);

                if (authorIndex == 0 && template.Author.Length == author.Length)
                {
                    return new MatchInfo(MatchInfo.DefaultParameter.Author, author, MatchKind.Exact);
                }

                if (authorIndex > -1)
                {
                    return new MatchInfo(MatchInfo.DefaultParameter.Author, author, MatchKind.Partial);
                }
                return new MatchInfo(MatchInfo.DefaultParameter.Author, author, MatchKind.Mismatch);
            };
        }

    }
}
