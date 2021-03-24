﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Mocks;
using Xunit;

namespace Microsoft.TemplateEngine.Edge.UnitTests
{
    public class WellKnownSearchFiltersTests
    {
        [Theory]
        [InlineData("test", "test", MatchKind.Exact)]
        [InlineData("test1||test2", "test1", MatchKind.Exact)]
        [InlineData("test1||test2", "test", MatchKind.Mismatch)]
        [InlineData("test1||test2", null, null)]
        public void TagFilterTests_TemplateWithTags (string templateTags, string testTag, MatchKind? kind)
        {
            const string separator = "||";
            string[] templateTagsArray = templateTags.Split(separator);

            MockTemplateInfo template = new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.T1", groupIdentity: "Console.App.Test")
                    .WithTag("language", "L1")
                    .WithTag("type", "project")
                    .WithBaselineInfo("app", "standard")
                    .WithClassifications(templateTagsArray);

            var filter = WellKnownSearchFilters.TagFilter(testTag);
            MatchInfo? result = filter(template);
            Assert.Equal(kind, result?.Kind);
        }

        [Theory]
        [InlineData("test", MatchKind.Mismatch)]
        [InlineData(null, null)]
        public void TagFilterTests_TemplateWithoutTags(string testTag, MatchKind? kind)
        {
            MockTemplateInfo template = new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.T1", groupIdentity: "Console.App.Test")
                    .WithTag("language", "L1")
                    .WithTag("type", "project")
                    .WithBaselineInfo("app", "standard");

            var filter = WellKnownSearchFilters.TagFilter(testTag);
            MatchInfo? result = filter(template);
            Assert.Equal(kind, result?.Kind);
        }

    }
}
