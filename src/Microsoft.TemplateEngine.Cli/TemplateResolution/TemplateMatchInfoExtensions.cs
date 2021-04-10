// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli.TemplateResolution
{
    public static class TemplateMatchInfoExtensions
    {
        public static bool HasDefaultLanguageMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(x => x.ParameterName == TemplateResolver.DefaultLanguageMatchParameterName && x.Kind == MatchKind.Exact);
        }

        public static bool HasAmbiguousParameterValueMatch(this ITemplateMatchInfo templateMatchInfo)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            return templateMatchInfo.MatchDisposition.Any(x => x.Kind == MatchKind.AmbiguousParameterValue);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public static bool IsInvokableMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Count > 0
                            && templateMatchInfo.MatchDisposition.All(x =>
                                x.Kind == MatchKind.Exact
                                ||
                                    // these locations can have partial or exact matches.
                                    x.Kind == MatchKind.Partial
                                    && (x.ParameterName == MatchInfo.DefaultParameter.Name
                                        || x.ParameterName == MatchInfo.DefaultParameter.ShortName
                                        || x.ParameterName == MatchInfo.DefaultParameter.Classification
                                        || x.ParameterName == MatchInfo.DefaultParameter.Author)
#pragma warning disable CS0618 // Type or member is obsolete
                                || x.Kind == MatchKind.SingleStartsWith);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        // This is analogous to INewCommandInput.InputTemplateParams
        public static IReadOnlyDictionary<string, string?> GetValidTemplateParameters(this ITemplateMatchInfo templateMatchInfo)
        {
            string[] nonTemplateParameterList = new[]
            {
                MatchInfo.DefaultParameter.Name,
                MatchInfo.DefaultParameter.ShortName,
                MatchInfo.DefaultParameter.Author,
                MatchInfo.DefaultParameter.Baseline,
                MatchInfo.DefaultParameter.Classification,
                MatchInfo.DefaultParameter.Type,
                MatchInfo.DefaultParameter.Language,
                "DefaultLanguage",
            };

            return templateMatchInfo.MatchDisposition.Where(
                x => !nonTemplateParameterList.Contains(x.ParameterName)
#pragma warning disable CS0618 // Type or member is obsolete
                     && (x.Kind == MatchKind.Exact || x.Kind == MatchKind.SingleStartsWith))
#pragma warning restore CS0618 // Type or member is obsolete
                .ToDictionary(x => x.ParameterName, x => x.ParameterValue);
        }

        public static bool HasNameMatchOrPartialMatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any((x => (x.ParameterName == MatchInfo.DefaultParameter.Name || x.ParameterName == MatchInfo.DefaultParameter.ShortName) && (x.Kind == MatchKind.Exact || x.Kind == MatchKind.Partial)));
        }

        public static bool HasAnyMismatch(this ITemplateMatchInfo templateMatchInfo)
        {
            return templateMatchInfo.MatchDisposition.Any(m => m.Kind == MatchKind.Mismatch);
        }
    }
}
