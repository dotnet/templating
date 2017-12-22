using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Cli.UnitTests.CliMocks;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;
using Xunit;
using static Microsoft.TemplateEngine.Cli.TemplateListResolutionResult;

namespace Microsoft.TemplateEngine.Cli.UnitTests.TemplateResolutionTests
{
    public class SingularInvokableMatchTests
    {
        [Fact(DisplayName = nameof(MultipleTemplatesInGroupHavingSingleStartsWithOnSameParamIsAmbiguous))]
        public void MultipleTemplatesInGroupHavingSingleStartsWithOnSameParamIsAmbiguous()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test_1",
                GroupIdentity = "foo.test.template",
                Precedence = 100,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "MyChoice", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "value_1"}) }
                },
                CacheParameters = new Dictionary<string, ICacheParameter>(),
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test_2",
                GroupIdentity = "foo.test.template",
                Precedence = 200,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "MyChoice", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "value_2"}) }
                },
                CacheParameters = new Dictionary<string, ICacheParameter>(),
            });

            INewCommandInput userInputs = new MockNewCommandInput(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "MyChoice", "value_" }
                }
            )
            {
                TemplateName = "foo"
            };

            IHostSpecificDataLoader hostSpecificDataLoader = new MockHostSpecificDataLoader();
            TemplateListResolutionResult matchResult = TemplateListResolver.GetTemplateResolutionResult(templatesToSearch, hostSpecificDataLoader, userInputs, null);
            // make sure there's an unambiguous group, otherwise the singular match check is meaningless
            Assert.True(matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyList<ITemplateMatchInfo> unambiguousGroup));
            Assert.Equal(2, unambiguousGroup.Count);
            Assert.Equal(2, matchResult.GetBestTemplateMatchList().Count);

            Assert.False(matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo singularInvokableMatch, out SingularInvokableMatchCheckStatus resultStatus));
            Assert.Equal(SingularInvokableMatchCheckStatus.AmbiguousChoice, resultStatus);
            Assert.Null(singularInvokableMatch);
        }

        [Fact(DisplayName = nameof(MultipleTemplatesInGroupParamPartiaMatch_TheOneHavingSingleStartsWithIsTheSingularInvokableMatch))]
        public void MultipleTemplatesInGroupParamPartiaMatch_TheOneHavingSingleStartsWithIsTheSingularInvokableMatch()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test_1",
                GroupIdentity = "foo.test.template",
                Precedence = 100,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "MyChoice", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "value_1"}) }
                },
                CacheParameters = new Dictionary<string, ICacheParameter>(),
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test_2",
                GroupIdentity = "foo.test.template",
                Precedence = 200,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "MyChoice", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "value_2", "value_3"}) }
                },
                CacheParameters = new Dictionary<string, ICacheParameter>(),
            });

            INewCommandInput userInputs = new MockNewCommandInput(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "MyChoice", "value_" }
                }
            )
            {
                TemplateName = "foo"
            };

            IHostSpecificDataLoader hostSpecificDataLoader = new MockHostSpecificDataLoader();
            TemplateListResolutionResult matchResult = TemplateListResolver.GetTemplateResolutionResult(templatesToSearch, hostSpecificDataLoader, userInputs, null);
            // make sure there's an unambiguous group, otherwise the singular match check is meaningless
            Assert.True(matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyList<ITemplateMatchInfo> unambiguousGroup));
            Assert.Equal(2, unambiguousGroup.Count);
            Assert.Equal(2, matchResult.GetBestTemplateMatchList().Count);

            Assert.True(matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo singularInvokableMatch, out SingularInvokableMatchCheckStatus resultStatus));
            Assert.Equal(SingularInvokableMatchCheckStatus.SingleMatch, resultStatus);
            Assert.Equal("foo.test_1", singularInvokableMatch.Info.Identity);
        }

        [Fact(DisplayName = nameof(MultipleTemplatesInGroupHavingAmbiguousParamMatchOnSameParamIsAmbiguous))]
        public void MultipleTemplatesInGroupHavingAmbiguousParamMatchOnSameParamIsAmbiguous()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test_1",
                GroupIdentity = "foo.test.template",
                Precedence = 100,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "MyChoice", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "value_1", "value_2"}) }
                },
                CacheParameters = new Dictionary<string, ICacheParameter>(),
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test_2",
                GroupIdentity = "foo.test.template",
                Precedence = 200,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "MyChoice", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "value_3", "value_4"}) }
                },
                CacheParameters = new Dictionary<string, ICacheParameter>(),
            });

            INewCommandInput userInputs = new MockNewCommandInput(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "MyChoice", "value_" }
                }
            )
            {
                TemplateName = "foo"
            };

            IHostSpecificDataLoader hostSpecificDataLoader = new MockHostSpecificDataLoader();
            TemplateListResolutionResult matchResult = TemplateListResolver.GetTemplateResolutionResult(templatesToSearch, hostSpecificDataLoader, userInputs, null);
            // make sure there's an unambiguous group, otherwise the singular match check is meaningless
            Assert.True(matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyList<ITemplateMatchInfo> unambiguousGroup));
            Assert.Equal(2, unambiguousGroup.Count);
            Assert.Equal(2, matchResult.GetBestTemplateMatchList().Count);

            Assert.False(matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo singularInvokableMatch, out SingularInvokableMatchCheckStatus resultStatus));
            Assert.Equal(SingularInvokableMatchCheckStatus.NoMatch, resultStatus);
            Assert.Null(singularInvokableMatch);
        }

        [Fact(DisplayName = nameof(MultipleTemplatesInGroupHavingSingularStartMatchesOnDifferentParams_HighPrecedenceIsChosen))]
        public void MultipleTemplatesInGroupHavingSingularStartMatchesOnDifferentParams_HighPrecedenceIsChosen()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test_1",
                GroupIdentity = "foo.test.template",
                Precedence = 100,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "MyChoice", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "value_1", "other_value"}) },    // single starts with
                    { "OtherChoice", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "foo_" }) }          // exact
                },
                CacheParameters = new Dictionary<string, ICacheParameter>(),
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test_2",
                GroupIdentity = "foo.test.template",
                Precedence = 200,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "MyChoice", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "value_" }) },    // exact
                    { "OtherChoice", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "foo_", "bar_1"}) }      // single starts with
                },
                CacheParameters = new Dictionary<string, ICacheParameter>(),
            });

            INewCommandInput userInputs = new MockNewCommandInput(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "MyChoice", "value_" },
                    { "OtherChoice", "foo_" }
                }
            )
            {
                TemplateName = "foo"
            };

            IHostSpecificDataLoader hostSpecificDataLoader = new MockHostSpecificDataLoader();
            TemplateListResolutionResult matchResult = TemplateListResolver.GetTemplateResolutionResult(templatesToSearch, hostSpecificDataLoader, userInputs, null);
            // make sure there's an unambiguous group, otherwise the singular match check is meaningless
            Assert.True(matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyList<ITemplateMatchInfo> unambiguousGroup));
            Assert.Equal(2, unambiguousGroup.Count);
            Assert.Equal(2, matchResult.GetBestTemplateMatchList().Count);

            Assert.True(matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo singularInvokableMatch, out SingularInvokableMatchCheckStatus resultStatus));
            Assert.Equal(SingularInvokableMatchCheckStatus.SingleMatch, resultStatus);
            Assert.Equal("foo.test_2", singularInvokableMatch.Info.Identity);
        }

        [Fact(DisplayName = nameof(GivenOneInvokableTemplateWithNonDefaultLanguage_ItIsChosen))]
        public void GivenOneInvokableTemplateWithNonDefaultLanguage_ItIsChosen()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test_1",
                GroupIdentity = "foo.test.template",
                Precedence = 100,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "language", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "F#" }) },
                },
                CacheParameters = new Dictionary<string, ICacheParameter>(),
            });

            INewCommandInput userInputs = new MockNewCommandInput(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { }
            )
            {
                TemplateName = "foo"
            };

            IHostSpecificDataLoader hostSpecificDataLoader = new MockHostSpecificDataLoader();
            TemplateListResolutionResult matchResult = TemplateListResolver.GetTemplateResolutionResult(templatesToSearch, hostSpecificDataLoader, userInputs, null);
            // make sure there's an unambiguous group, otherwise the singular match check is meaningless
            Assert.True(matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyList<ITemplateMatchInfo> unambiguousGroup));
            Assert.Equal(1, unambiguousGroup.Count);
            Assert.Equal(1, matchResult.GetBestTemplateMatchList().Count);

            Assert.True(matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo singularInvokableMatch, out SingularInvokableMatchCheckStatus resultStatus));
            Assert.Equal(SingularInvokableMatchCheckStatus.SingleMatch, resultStatus);
            Assert.Equal("foo.test_1", singularInvokableMatch.Info.Identity);
        }

        [Fact(DisplayName = nameof(GivenTwoInvokableTemplatesNonDefaultLanguage_HighPrecedenceIsChosen))]
        public void GivenTwoInvokableTemplatesNonDefaultLanguage_HighPrecedenceIsChosen()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test_1.FSharp",
                GroupIdentity = "foo.test.template",
                Precedence = 100,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "language", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "F#" }) },
                },
                CacheParameters = new Dictionary<string, ICacheParameter>(),
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test_1.VB",
                GroupIdentity = "foo.test.template",
                Precedence = 200,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "language", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "VB" }) },
                },
                CacheParameters = new Dictionary<string, ICacheParameter>(),
            });

            INewCommandInput userInputs = new MockNewCommandInput(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { }
            )
            {
                TemplateName = "foo"
            };


            IHostSpecificDataLoader hostSpecificDataLoader = new MockHostSpecificDataLoader();
            TemplateListResolutionResult matchResult = TemplateListResolver.GetTemplateResolutionResult(templatesToSearch, hostSpecificDataLoader, userInputs, null);
            // make sure there's an unambiguous group, otherwise the singular match check is meaningless
            Assert.True(matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyList<ITemplateMatchInfo> unambiguousGroup));
            Assert.Equal(2, unambiguousGroup.Count);
            Assert.Equal(2, matchResult.GetBestTemplateMatchList().Count);

            Assert.True(matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo singularInvokableMatch, out SingularInvokableMatchCheckStatus resultStatus));
            Assert.Equal(SingularInvokableMatchCheckStatus.SingleMatch, resultStatus);
            Assert.Equal("foo.test_1.VB", singularInvokableMatch.Info.Identity);
        }

        [Fact(DisplayName = nameof(GivenMultipleHighestPrecedenceTemplates_ResultIsAmbiguous))]
        public void GivenMultipleHighestPrecedenceTemplates_ResultIsAmbiguous()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test_1.FSharp",
                GroupIdentity = "foo.test.template",
                Precedence = 100,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "language", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "F#" }) },
                },
                CacheParameters = new Dictionary<string, ICacheParameter>(),
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test_1.VB",
                GroupIdentity = "foo.test.template",
                Precedence = 100,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "language", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "VB" }) },
                },
                CacheParameters = new Dictionary<string, ICacheParameter>(),
            });

            INewCommandInput userInputs = new MockNewCommandInput(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { }
            )
            {
                TemplateName = "foo"
            };

            IHostSpecificDataLoader hostSpecificDataLoader = new MockHostSpecificDataLoader();
            TemplateListResolutionResult matchResult = TemplateListResolver.GetTemplateResolutionResult(templatesToSearch, hostSpecificDataLoader, userInputs, null);
            // make sure there's an unambiguous group, otherwise the singular match check is meaningless
            Assert.True(matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyList<ITemplateMatchInfo> unambiguousGroup));
            Assert.Equal(2, unambiguousGroup.Count);
            Assert.Equal(2, matchResult.GetBestTemplateMatchList().Count);

            Assert.False(matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo singularInvokableMatch, out SingularInvokableMatchCheckStatus resultStatus));
            Assert.Null(singularInvokableMatch);
            Assert.Equal(SingularInvokableMatchCheckStatus.AmbiguousPrecedence, resultStatus);
        }
    }
}
