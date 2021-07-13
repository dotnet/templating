// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Cli.TemplateResolution;
using Microsoft.TemplateEngine.Cli.UnitTests.CliMocks;
using Microsoft.TemplateEngine.Mocks;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests.TemplateResolutionTests
{
    public class ListTemplateResolverTests
    {
        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_UniqueNameMatchesCorrectly))]
        public async Task TestGetTemplateResolutionResult_UniqueNameMatchesCorrectly()
        {
            IReadOnlyList<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>()
            {
                new MockTemplateInfo("console1", name: "Long name for Console App", identity: "Console.App"),
                new MockTemplateInfo("console2", name: "Long name for Console App #2", identity: "Console.App2")
            };

            INewCommandInput userInputs = new MockNewCommandInput("console2").WithListOption();
            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);

            Assert.True(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.NotNull(matchResult.UnambiguousTemplateGroup);
            Assert.Equal("console2", matchResult.UnambiguousTemplateGroup?.Templates.Single().ShortNameList.Single());
            Assert.Equal("Console.App2", matchResult.UnambiguousTemplateGroup?.Templates.Single().Identity);
            Assert.Equal(1, matchResult.UnambiguousTemplateGroup?.Templates.Count);
        }

        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_ExactMatchOnShortNameMatchesCorrectly))]
        public async Task TestGetTemplateResolutionResult_ExactMatchOnShortNameMatchesCorrectly()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App"));
            templatesToSearch.Add(new MockTemplateInfo("console2", name: "Long name for Console App #2", identity: "Console.App2"));

            INewCommandInput userInputs = new MockNewCommandInput("console").WithListOption();

            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);

            Assert.True(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.Null(matchResult.UnambiguousTemplateGroup);
            Assert.Equal(2, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
            Assert.NotNull(matchResult.TemplateGroupsWithMatchingTemplateInfo.SelectMany(group => group.Templates).Single(t => t.Identity == "Console.App"));
            Assert.NotNull(matchResult.TemplateGroupsWithMatchingTemplateInfo.SelectMany(group => group.Templates).Single(t => t.Identity == "Console.App2"));
        }

        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_UnambiguousGroupIsFound))]
        public async Task TestGetTemplateResolutionResult_UnambiguousGroupIsFound()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.L1", groupIdentity: "Console.App.Test").WithTag("language", "L1"));
            templatesToSearch.Add(new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.L2", groupIdentity: "Console.App.Test").WithTag("language", "L2"));
            templatesToSearch.Add(new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.L3", groupIdentity: "Console.App.Test").WithTag("language", "L3"));

            INewCommandInput userInputs = new MockNewCommandInput("console").WithListOption();

            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);

            Assert.True(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.Equal(1, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
            Assert.Equal(3, matchResult.TemplateGroupsWithMatchingTemplateInfo.Single().Templates.Count);
            Assert.NotNull(matchResult.UnambiguousTemplateGroup);
            Assert.Equal(3, matchResult.UnambiguousTemplateGroup?.Templates.Count);
        }

        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_MultipleGroupsAreFound))]
        public async Task TestGetTemplateResolutionResult_MultipleGroupsAreFound()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.L1", groupIdentity: "Console.App.Test").WithTag("language", "L1"));
            templatesToSearch.Add(new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.L2", groupIdentity: "Console.App.Test").WithTag("language", "L2"));
            templatesToSearch.Add(new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.L3", groupIdentity: "Console.App.Test").WithTag("language", "L3"));
            templatesToSearch.Add(new MockTemplateInfo("classlib", name: "Long name for Class Library App", identity: "Class.Library.L1", groupIdentity: "Class.Library.Test").WithTag("language", "L1"));
            templatesToSearch.Add(new MockTemplateInfo("classlib", name: "Long name for Class Library App", identity: "Class.Library.L2", groupIdentity: "Class.Library.Test").WithTag("language", "L2"));

            INewCommandInput userInputs = new MockNewCommandInput("c").WithListOption();

            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);

            Assert.True(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.Equal(2, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
            Assert.Equal(5, matchResult.TemplateGroupsWithMatchingTemplateInfo.SelectMany(group => group.Templates).Count());
            Assert.Null(matchResult.UnambiguousTemplateGroup);
        }

        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_DefaultLanguageDisambiguates))]
        public async Task TestGetTemplateResolutionResult_DefaultLanguageDisambiguates()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.L1", groupIdentity: "Console.App.Test").WithTag("language", "L1"));
            templatesToSearch.Add(new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.L2", groupIdentity: "Console.App.Test").WithTag("language", "L2"));

            INewCommandInput userInputs = new MockNewCommandInput("console").WithListOption();
            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: "L1", default).ConfigureAwait(false);

            Assert.True(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.NotNull(matchResult.UnambiguousTemplateGroup);
            Assert.Equal(1, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
            Assert.Equal(2, matchResult.TemplateGroupsWithMatchingTemplateInfo.Single().Templates.Count);
            Assert.NotNull(matchResult.TemplateGroupsWithMatchingTemplateInfo.Single().Templates.Single(t => t.Identity == "Console.App.L1"));
            Assert.NotNull(matchResult.TemplateGroupsWithMatchingTemplateInfo.Single().Templates.Single(t => t.Identity == "Console.App.L2"));
        }

        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_InputLanguageIsPreferredOverDefault))]
        public async Task TestGetTemplateResolutionResult_InputLanguageIsPreferredOverDefault()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.L1", groupIdentity: "Console.App.Test").WithTag("language", "L1"));
            templatesToSearch.Add(new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.L2", groupIdentity: "Console.App.Test").WithTag("language", "L2"));

            INewCommandInput userInputs = new MockNewCommandInput("console", "L2").WithListOption();
            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: "L1", default).ConfigureAwait(false);

            Assert.True(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.NotNull(matchResult.UnambiguousTemplateGroup);
            Assert.Equal(2, matchResult.TemplateGroupsWithMatchingTemplateInfo?.Single().Templates.Count);
            Assert.Equal(2, matchResult.UnambiguousTemplateGroup?.Templates.Count);
        }

        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_PartialMatch_HasLanguageMismatch))]
        public async Task TestGetTemplateResolutionResult_PartialMatch_HasLanguageMismatch()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(
              new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.T1", groupIdentity: "Console.App.Test")
                  .WithTag("language", "L1")
                  .WithTag("type", "project")
                  .WithBaselineInfo("app", "standard"));

            INewCommandInput userInputs = new MockNewCommandInput("console", "L2").WithListOption();
            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);

            Assert.False(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.Equal(0, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
            Assert.Equal(1, matchResult.TemplateGroups.Count());
            Assert.Equal(1, matchResult.TemplateGroups.Single().Templates.Count);
            Assert.True(matchResult.HasLanguageMismatch);
            Assert.False(matchResult.HasTypeMismatch);
            Assert.False(matchResult.HasBaselineMismatch);
            Assert.Null(matchResult.UnambiguousTemplateGroup);
        }

        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_PartialMatch_HasContextMismatch))]
        public async Task TestGetTemplateResolutionResult_PartialMatch_HasContextMismatch()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(
                   new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.T1", groupIdentity: "Console.App.Test")
                       .WithTag("language", "L1")
                       .WithTag("type", "project")
                       .WithBaselineInfo("app", "standard"));

            INewCommandInput userInputs = new MockNewCommandInput("console", type: "item").WithListOption();
            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);

            Assert.False(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.Equal(0, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
            Assert.Equal(1, matchResult.TemplateGroups.Count());
            Assert.Equal(1, matchResult.TemplateGroups.Single().Templates.Count);
            Assert.False(matchResult.HasLanguageMismatch);
            Assert.True(matchResult.HasTypeMismatch);
            Assert.False(matchResult.HasBaselineMismatch);
            Assert.Null(matchResult.UnambiguousTemplateGroup);
        }

        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_PartialMatch_HasBaselineMismatch))]
        public async Task TestGetTemplateResolutionResult_PartialMatch_HasBaselineMismatch()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(
                   new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.T1", groupIdentity: "Console.App.Test")
                       .WithTag("language", "L1")
                       .WithTag("type", "project")
                       .WithBaselineInfo("app", "standard"));

            INewCommandInput userInputs = new MockNewCommandInput("console").WithListOption().WithCommandOption("--baseline", "core");

            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);
            Assert.False(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.Equal(0, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
            Assert.Equal(1, matchResult.TemplateGroups.Count());
            Assert.Equal(1, matchResult.TemplateGroups.Single().Templates.Count);
            Assert.False(matchResult.HasLanguageMismatch);
            Assert.False(matchResult.HasTypeMismatch);
            Assert.True(matchResult.HasBaselineMismatch);
            Assert.Null(matchResult.UnambiguousTemplateGroup);
        }

        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_PartialMatch_HasMultipleMismatches))]
        public async Task TestGetTemplateResolutionResult_PartialMatch_HasMultipleMismatches()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(
                   new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.T1", groupIdentity: "Console.App.Test")
                       .WithTag("language", "L1")
                       .WithTag("type", "project")
                       .WithBaselineInfo("app", "standard"));

            INewCommandInput userInputs = new MockNewCommandInput("console", "L2", "item").WithListOption().WithCommandOption("--baseline", "core");

            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);
            Assert.False(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.Equal(0, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
            Assert.Equal(1, matchResult.TemplateGroups.Count());
            Assert.Equal(1, matchResult.TemplateGroups.Single().Templates.Count);
            Assert.True(matchResult.HasLanguageMismatch);
            Assert.True(matchResult.HasTypeMismatch);
            Assert.True(matchResult.HasBaselineMismatch);
            Assert.Null(matchResult.UnambiguousTemplateGroup);
        }

        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_NoMatch))]
        public async Task TestGetTemplateResolutionResult_NoMatch()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(
                   new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.T1", groupIdentity: "Console.App.Test")
                       .WithTag("language", "L1")
                       .WithTag("type", "project")
                       .WithBaselineInfo("app", "standard"));

            INewCommandInput userInputs = new MockNewCommandInput("zzzzz", "L1", "project").WithListOption().WithCommandOption("--baseline", "app");

            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);
            Assert.False(matchResult.HasTemplateGroupMatches);
            Assert.False(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.Equal(0, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
            Assert.Equal(0, matchResult.TemplateGroups.Count());
            Assert.False(matchResult.HasLanguageMismatch);
            Assert.False(matchResult.HasTypeMismatch);
            Assert.False(matchResult.HasBaselineMismatch);
            Assert.False(matchResult.HasClassificationMismatch);
            Assert.Null(matchResult.UnambiguousTemplateGroup);
        }

        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_MatchByTags))]
        public async Task TestGetTemplateResolutionResult_MatchByTags()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(
                new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.T1", groupIdentity: "Console.App.Test1")
                    .WithTag("language", "L1")
                    .WithTag("type", "project")
                    .WithClassifications("Common", "Test")
                    .WithBaselineInfo("app", "standard"));

            INewCommandInput userInputs = new MockNewCommandInput().WithListOption().WithCommandOption("--tag", "Common");

            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);
            Assert.True(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.Equal(1, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
            Assert.Equal(1, matchResult.TemplateGroupsWithMatchingTemplateInfo.Single().Templates.Count);
            Assert.False(matchResult.HasLanguageMismatch);
            Assert.False(matchResult.HasTypeMismatch);
            Assert.False(matchResult.HasBaselineMismatch);
            Assert.False(matchResult.HasClassificationMismatch);
        }

        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_MatchByTagsIgnoredOnNameMatch))]
        public async Task TestGetTemplateResolutionResult_MatchByTagsIgnoredOnNameMatch()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(
                new MockTemplateInfo("console1", name: "Long name for Console App Test", identity: "Console.App.T1", groupIdentity: "Console.App.Test")
                    .WithTag("language", "L1")
                    .WithTag("type", "project")
                    .WithClassifications("Common", "Test")
                    .WithBaselineInfo("app", "standard"));
            templatesToSearch.Add(
              new MockTemplateInfo("console2", name: "Long name for Console App", identity: "Console.App.T2", groupIdentity: "Console.App.Test2")
                  .WithTag("language", "L1")
                  .WithTag("type", "project")
                  .WithClassifications("Common", "Test")
                  .WithBaselineInfo("app", "standard"));

            INewCommandInput userInputs = new MockNewCommandInput("Test").WithListOption();

            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);
            Assert.True(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.Equal(1, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
            Assert.Equal(1, matchResult.TemplateGroupsWithMatchingTemplateInfo.Single().Templates.Count);
            Assert.False(matchResult.HasLanguageMismatch);
            Assert.False(matchResult.HasTypeMismatch);
            Assert.False(matchResult.HasBaselineMismatch);
            Assert.Equal("console1", matchResult.UnambiguousTemplateGroup?.Templates.Single().ShortNameList.Single());
            Assert.Equal("Console.App.T1", matchResult.UnambiguousTemplateGroup?.Templates.Single().Identity);
        }

        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_MatchByTagsIgnoredOnShortNameMatch))]
        public async Task TestGetTemplateResolutionResult_MatchByTagsIgnoredOnShortNameMatch()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(
              new MockTemplateInfo("console", name: "Long name for Console App Test", identity: "Console.App.T1", groupIdentity: "Console.App.Test")
                  .WithTag("language", "L1")
                  .WithTag("type", "project")
                  .WithClassifications("Common", "Test", "Console")
                  .WithBaselineInfo("app", "standard"));
            templatesToSearch.Add(
              new MockTemplateInfo("cons", name: "Long name for Cons App", identity: "Console.App.T2", groupIdentity: "Console.App.Test2")
                  .WithTag("language", "L1")
                  .WithTag("type", "project")
                  .WithClassifications("Common", "Test", "Console")
                  .WithBaselineInfo("app", "standard"));

            INewCommandInput userInputs = new MockNewCommandInput("Console").WithListOption();

            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);
            Assert.True(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.Equal(1, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
            Assert.Equal(1, matchResult.TemplateGroupsWithMatchingTemplateInfo.Single().Templates.Count);
            Assert.False(matchResult.HasLanguageMismatch);
            Assert.False(matchResult.HasTypeMismatch);
            Assert.False(matchResult.HasBaselineMismatch);
            Assert.Equal("console", matchResult.UnambiguousTemplateGroup?.Templates.Single().ShortNameList.Single());
            Assert.Equal("Console.App.T1", matchResult.UnambiguousTemplateGroup?.Templates.Single().Identity);
        }

        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_MatchByTagsAndMismatchByOtherFilter))]
        public async Task TestGetTemplateResolutionResult_MatchByTagsAndMismatchByOtherFilter()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(
               new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.T1", groupIdentity: "Console.App.Test")
                   .WithTag("language", "L1")
                   .WithTag("type", "project")
                   .WithClassifications("Common", "Test")
                   .WithBaselineInfo("app", "standard"));

            INewCommandInput userInputs = new MockNewCommandInput()
                .WithListOption()
                .WithCommandOption("--tag", "Common")
                .WithCommandOption("--language", "L2")
                .WithCommandOption("--type", "item");

            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);
            Assert.False(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.Equal(0, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
            Assert.True(matchResult.HasTemplateGroupMatches);
            Assert.Equal(1, matchResult.TemplateGroups.Count());
            Assert.Equal(1, matchResult.TemplateGroups.Single().Templates.Count);
            Assert.True(matchResult.HasLanguageMismatch);
            Assert.True(matchResult.HasTypeMismatch);
            Assert.False(matchResult.HasBaselineMismatch);
        }

        [Theory(DisplayName = nameof(TestGetTemplateResolutionResult_AuthorMatch))]
        [InlineData("TestAuthor", "Test", true)]
        [InlineData("TestAuthor", "Other", false)]
        [InlineData("TestAuthor", "", true)]
        [InlineData("TestAuthor", null, true)]
        [InlineData("TestAuthor", "TeST", true)]
        [InlineData("TestAuthor", "Teşt", false)]
        [InlineData("match_middle_test", "middle", true)]
        [InlineData("input", "İnput", false)]
        public async Task TestGetTemplateResolutionResult_AuthorMatch(string templateAuthor, string commandAuthor, bool matchExpected)
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();

            templatesToSearch.Add(
               new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.T1", groupIdentity: "Console.App.Test", author: templateAuthor)
                   .WithTag("language", "L1")
                   .WithTag("type", "project")
                   .WithClassifications("Common", "Test")
                   .WithBaselineInfo("app", "standard"));

            INewCommandInput userInputs = new MockNewCommandInput("console").WithListOption().WithCommandOption("--author", commandAuthor);

            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);

            if (matchExpected)
            {
                Assert.True(matchResult.HasTemplateGroupWithTemplateInfoMatches);
                Assert.Equal(1, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
                Assert.Equal(1, matchResult.TemplateGroupsWithMatchingTemplateInfo.Single().Templates.Count);
                Assert.False(matchResult.HasAuthorMismatch);
            }
            else
            {
                Assert.False(matchResult.HasTemplateGroupWithTemplateInfoMatches);
                Assert.Equal(0, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
                Assert.True(matchResult.HasTemplateGroupMatches);
                Assert.Equal(1, matchResult.TemplateGroups.Count());
                Assert.Equal(1, matchResult.TemplateGroups.Single().Templates.Count);
                Assert.True(matchResult.HasAuthorMismatch);
            }

            Assert.False(matchResult.HasLanguageMismatch);
            Assert.False(matchResult.HasTypeMismatch);
            Assert.False(matchResult.HasBaselineMismatch);
        }

        [Theory(DisplayName = nameof(TestGetTemplateResolutionResult_TagsMatch))]
        [InlineData("TestTag", "TestTag", true)]
        [InlineData("Tag1||Tag2", "Tag1", true)]
        [InlineData("Tag1||Tag2", "Tag", false)]
        [InlineData("", "Tag", false)]
        [InlineData("TestTag", "Other", false)]
        [InlineData("TestTag", "", true)]
        [InlineData("TestTag", null, true)]
        [InlineData("TestTag", "TeSTTag", true)]
        [InlineData("TestTag", "TeştTag", false)]
        [InlineData("match_middle_test", "middle", false)]
        [InlineData("input", "İnput", false)]
        public async Task TestGetTemplateResolutionResult_TagsMatch(string templateTags, string commandTag, bool matchExpected)
        {
            const string separator = "||";
            string[] templateTagsArray = templateTags.Split(separator);

            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();

            templatesToSearch.Add(
               new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.T1", groupIdentity: "Console.App.Test", author: "TemplateAuthor")
                   .WithTag("language", "L1")
                   .WithTag("type", "project")
                   .WithClassifications(templateTagsArray)
                   .WithBaselineInfo("app", "standard"));

            MockNewCommandInput userInputs = new MockNewCommandInput("console").WithListOption().WithCommandOption("--tag", commandTag);

            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);

            if (matchExpected)
            {
                Assert.True(matchResult.HasTemplateGroupWithTemplateInfoMatches);
                Assert.Equal(1, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
                Assert.Equal(1, matchResult.TemplateGroupsWithMatchingTemplateInfo.Single().Templates.Count);
                Assert.False(matchResult.HasClassificationMismatch);
            }
            else
            {
                Assert.False(matchResult.HasTemplateGroupWithTemplateInfoMatches);
                Assert.Equal(0, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
                Assert.True(matchResult.HasTemplateGroupMatches);
                Assert.Equal(1, matchResult.TemplateGroups.Count());
                Assert.Equal(1, matchResult.TemplateGroups.Single().Templates.Count);
                Assert.True(matchResult.HasClassificationMismatch);
            }

            Assert.False(matchResult.HasLanguageMismatch);
            Assert.False(matchResult.HasTypeMismatch);
            Assert.False(matchResult.HasBaselineMismatch);
            Assert.False(matchResult.HasAuthorMismatch);
        }

        [Fact(DisplayName = nameof(TestGetTemplateResolutionResult_TemplateWithoutTypeShouldNotBeMatchedForContextFilter))]
        public async Task TestGetTemplateResolutionResult_TemplateWithoutTypeShouldNotBeMatchedForContextFilter()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(
                new MockTemplateInfo("console", name: "Long name for Console App", identity: "Console.App.T1", groupIdentity: "Console.App.Test")
                    .WithClassifications("Common", "Test"));

            INewCommandInput userInputs = new MockNewCommandInput("console", type: "item").WithListOption();

            ListTemplateResolver resolver = new ListTemplateResolver(templatesToSearch, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(userInputs, defaultLanguage: null, default).ConfigureAwait(false);
            Assert.False(matchResult.HasTemplateGroupWithTemplateInfoMatches);
            Assert.Equal(0, matchResult.TemplateGroupsWithMatchingTemplateInfo.Count());
            Assert.True(matchResult.HasTemplateGroupMatches);
            Assert.Equal(1, matchResult.TemplateGroups.Count());
            Assert.Equal(1, matchResult.TemplateGroups.Single().Templates.Count);
            Assert.False(matchResult.HasLanguageMismatch);
            Assert.True(matchResult.HasTypeMismatch);
            Assert.False(matchResult.HasBaselineMismatch);
        }
    }
}
