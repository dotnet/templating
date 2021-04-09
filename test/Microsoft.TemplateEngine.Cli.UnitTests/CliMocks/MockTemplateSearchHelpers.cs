// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateSearch.Common;

namespace Microsoft.TemplateEngine.Cli.UnitTests.CliMocks
{
    internal static class MockTemplateSearchHelpers
    {
        private const string DefaultNameValue = "test";
        private static IReadOnlyList<MatchInfo> s_defaultMatchInfo = new List<MatchInfo>()
        {
            new MatchInfo(MatchInfo.DefaultParameter.Name, DefaultNameValue,  MatchKind.Exact)
        };

        public static Func<IReadOnlyList<ITemplateNameSearchResult>, IReadOnlyList<ITemplateMatchInfo>> DefaultMatchFilter => (nameMatches) =>
        {
            return nameMatches.Select(match => new TemplateMatchInfo(match.Template, s_defaultMatchInfo)).ToList();
        };
    }
}
