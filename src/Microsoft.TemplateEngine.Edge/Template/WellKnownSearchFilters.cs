// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Edge.Template
{
    public static class WellKnownSearchFilters
    {
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

        // This being case-insensitive depends on the dictionaries on the cache tags being declared as case-insensitive
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
        /// Creates predicate for matching the template and given tag value.
        /// If the template contains the tag <paramref name="tagFilter"/>, it is exact match, otherwise mismatch.
        /// If the template has no tags defined, it is a mismatch.
        /// If <paramref name="tagFilter"/> is <see cref="null"/> or empty the method returns <see cref="null"/>.
        /// </summary>
        /// <param name="tagFilter">tag to filter by.</param>
        /// <returns>A predicate that returns if the given template matches <paramref name="tagFilter"/>.</returns>
        public static Func<ITemplateInfo, MatchInfo?> TagFilter(string tagFilter)
        {
            return (template) =>
            {
                if (string.IsNullOrWhiteSpace(tagFilter))
                {
                    return null;
                }
                if (template.Classifications?.Contains(tagFilter, StringComparer.OrdinalIgnoreCase) ?? false)
                {
                    return new MatchInfo(MatchInfo.DefaultParameter.Classification, tagFilter, MatchKind.Exact);
                }
                return new MatchInfo(MatchInfo.DefaultParameter.Classification, tagFilter, MatchKind.Mismatch);
            };
        }

        // This being case-insensitive depends on the dictionaries on the cache tags being declared as case-insensitive
        // Note: This is specifically designed to provide match info against a user-input language.
        //      All dealings with the host-default language should be separate from this.
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

        [Obsolete("Use TagsFilter instead")]
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
        /// Creates predicate for matching the template and given author value.
        /// </summary>
        /// <param name="author">author to use for match.</param>
        /// <returns>A predicate that returns if the given template matches defined author.</returns>
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
