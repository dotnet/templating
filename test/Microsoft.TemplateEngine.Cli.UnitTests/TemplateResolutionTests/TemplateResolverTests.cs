// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Cli.TemplateResolution;
using Microsoft.TemplateEngine.Cli.UnitTests.CliMocks;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Utils;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests.TemplateResolutionTests
{
    // Implementation notes:
    // If a test is going to hit the secondary matching in the resolver, make sure to initialize the Tags & CacheParameters,
    //  otherwise an exception will be thrown in TemplateInfo.Parameters getter
    //  (just about every situation will get to the secondary matching)
    // MockNewCommandInput doesn't support everything in the interface, just enough for this type of testing.
    public class TemplateResolverTests
    {
        [Fact(DisplayName = nameof(TestTryGetHighestPrecedenceTemplate))]
        public void TestTryGetHighestPrecedenceTemplate()
        {
            List<ITemplateMatchInfo> templatesToCheck = new List<ITemplateMatchInfo>();
            templatesToCheck.Add(new TemplateMatchInfo(
                new TemplateInfo()
                {
                    Precedence = 10,
                    Name = "Template1",
                    ShortName = "Template",
                    Identity = "Template1",
                    GroupIdentity = "TestGroup"
                },
                new List<MatchInfo>
                {
                    new MatchInfo
                    {
                         Location = MatchLocation.ShortName,
                         Kind = MatchKind.Exact
                    }
                }
                ));
            templatesToCheck.Add(new TemplateMatchInfo(
                new TemplateInfo()
                {
                    Precedence = 20,
                    Name = "Template2",
                    ShortName = "Template",
                    Identity = "Template2",
                    GroupIdentity = "TestGroup"
                },
                new List<MatchInfo>
                {
                    new MatchInfo
                    {
                         Location = MatchLocation.ShortName,
                         Kind = MatchKind.Exact
                    }
                }
                ));
            templatesToCheck.Add(new TemplateMatchInfo(
                new TemplateInfo()
                {
                    Precedence = 0,
                    Name = "Template3",
                    ShortName = "Template",
                    Identity = "Template3",
                    GroupIdentity = "TestGroup"
                },
                new List<MatchInfo>
                {
                    new MatchInfo
                    {
                         Location = MatchLocation.ShortName,
                         Kind = MatchKind.Exact
                    }
                }
                ));

            TemplateGroup templateGroup = new TemplateGroup("TestGroup", templatesToCheck);
            Assert.True(templateGroup.TryGetHighestPrecedenceInvokableTemplate(out ITemplateMatchInfo highestPrecedenceTemplate));
            Assert.NotNull(highestPrecedenceTemplate);
            Assert.Equal("Template2", highestPrecedenceTemplate.Info.Identity);
            Assert.Equal(20, highestPrecedenceTemplate.Info.Precedence);
        }

        [Fact(DisplayName = nameof(TestTryGetHighestPrecedenceTemplate_ReturnsNullIfPrecedenceIsTheSame))]
        public void TestTryGetHighestPrecedenceTemplate_ReturnsNullIfPrecedenceIsTheSame()
        {
            List<ITemplateMatchInfo> templatesToCheck = new List<ITemplateMatchInfo>();
            templatesToCheck.Add(new TemplateMatchInfo(
                new TemplateInfo()
                {
                    Precedence = 10,
                    Name = "Template1",
                    Identity = "Template1",
                    GroupIdentity = "TestGroup"
                },
                new List<MatchInfo>
                {
                    new MatchInfo
                    {
                         Location = MatchLocation.ShortName,
                         Kind = MatchKind.Exact
                    }
                }
                ));
            templatesToCheck.Add(new TemplateMatchInfo(
                new TemplateInfo()
                {
                    Precedence = 10,
                    Name = "Template2",
                    Identity = "Template2",
                    GroupIdentity = "TestGroup"
                },
                new List<MatchInfo>
                {
                    new MatchInfo
                    {
                         Location = MatchLocation.ShortName,
                         Kind = MatchKind.Exact
                    }
                }
                ));
            TemplateGroup templateGroup = new TemplateGroup("TestGroup", templatesToCheck);
            Assert.False(templateGroup.TryGetHighestPrecedenceInvokableTemplate(out ITemplateMatchInfo highestPrecedenceTemplate));
            Assert.Null(highestPrecedenceTemplate);
        }

        [Fact(DisplayName = nameof(TestPerformAllTemplatesInContextQuery))]
        public void TestPerformAllTemplatesInContextQuery()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                Name = "Template1",
                Identity = "Template1",
                CacheParameters = new Dictionary<string, ICacheParameter>(),
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "type", ResolutionTestHelper.CreateTestCacheTag("project") }
                },
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                Name = "Template2",
                Identity = "Template2",
                CacheParameters = new Dictionary<string, ICacheParameter>(),
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "type", ResolutionTestHelper.CreateTestCacheTag("item") }
                }
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                Name = "Template3",
                Identity = "Template3",
                CacheParameters = new Dictionary<string, ICacheParameter>(),
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "type", ResolutionTestHelper.CreateTestCacheTag("myType") }
                }
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                Name = "Template4",
                Identity = "Template4",
                CacheParameters = new Dictionary<string, ICacheParameter>(),
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "type",ResolutionTestHelper.CreateTestCacheTag("project") }
                }
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                Name = "Template5",
                Identity = "Template5",
                CacheParameters = new Dictionary<string, ICacheParameter>(),
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "type", ResolutionTestHelper.CreateTestCacheTag("project") }
                }
            });

            IHostSpecificDataLoader hostDataLoader = new MockHostSpecificDataLoader();

            IReadOnlyCollection<ITemplateMatchInfo> projectTemplates = TemplateResolver.PerformAllTemplatesInContextQuery(templatesToSearch, hostDataLoader, "project");
            Assert.Equal(3, projectTemplates.Count);
            Assert.True(projectTemplates.Where(x => string.Equals(x.Info.Identity, "Template1", StringComparison.Ordinal)).Any());
            Assert.True(projectTemplates.Where(x => string.Equals(x.Info.Identity, "Template4", StringComparison.Ordinal)).Any());
            Assert.True(projectTemplates.Where(x => string.Equals(x.Info.Identity, "Template5", StringComparison.Ordinal)).Any());

            IReadOnlyCollection<ITemplateMatchInfo> itemTemplates = TemplateResolver.PerformAllTemplatesInContextQuery(templatesToSearch, hostDataLoader, "item");
            Assert.Equal(1, itemTemplates.Count);
            Assert.True(itemTemplates.Where(x => string.Equals(x.Info.Identity, "Template2", StringComparison.Ordinal)).Any());

            //Visual Studio only supports "project" and "item", so using other types is no longer allowed, therefore "other" handling is removed.
            //support of match on custom type still remains
            IReadOnlyCollection<ITemplateMatchInfo> otherTemplates = TemplateResolver.PerformAllTemplatesInContextQuery(templatesToSearch, hostDataLoader, "other");
            Assert.Equal(0, otherTemplates.Count);
            Assert.False(otherTemplates.Where(x => string.Equals(x.Info.Identity, "Template3", StringComparison.Ordinal)).Any());

            IReadOnlyCollection<ITemplateMatchInfo> customTypeTemplates = TemplateResolver.PerformAllTemplatesInContextQuery(templatesToSearch, hostDataLoader, "myType");
            Assert.Equal(1, customTypeTemplates.Count);
            Assert.True(customTypeTemplates.Where(x => string.Equals(x.Info.Identity, "Template3", StringComparison.Ordinal)).Any());
        }

        [Fact(DisplayName = nameof(TestPerformCoreTemplateQuery_UniqueNameMatchesCorrectly))]
        public void TestPerformCoreTemplateQuery_UniqueNameMatchesCorrectly()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "Template1",
                Name = "Long name of Template1",
                Identity = "Template1",
                CacheParameters = new Dictionary<string, ICacheParameter>(),
                Tags = new Dictionary<string, ICacheTag>()
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "Template2",
                Name = "Long name of Template2",
                Identity = "Template2",
                CacheParameters = new Dictionary<string, ICacheParameter>(),
                Tags = new Dictionary<string, ICacheTag>()
            });

            INewCommandInput userInputs = new MockNewCommandInput()
            {
                TemplateName = "Template2"
            };

            TemplateResolutionResult matchResult = TemplateResolver.GetTemplateResolutionResult(templatesToSearch, new MockHostSpecificDataLoader(), userInputs, null);
            Assert.True(matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyCollection<ITemplateMatchInfo> unambiguousGroup));
            Assert.Equal(1, unambiguousGroup.Count);
            Assert.Equal("Template2", unambiguousGroup.First().Info.Identity);
            Assert.True(matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo templateToInvoke, out TemplateResolutionResult.SingleInvokableMatchStatus resultStatus));
            Assert.Equal("Template2", templateToInvoke.Info.Identity);
            Assert.Equal(TemplateResolutionResult.SingleInvokableMatchStatus.SingleMatch, resultStatus);
        }

        [Fact(DisplayName = nameof(TestPerformCoreTemplateQuery_DefaultLanguageDisambiguates))]
        public void TestPerformCoreTemplateQuery_DefaultLanguageDisambiguates()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Description of foo Perl template",
                Identity = "foo.test.Perl",
                GroupIdentity = "foo.test.template",
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "language", ResolutionTestHelper.CreateTestCacheTag("Perl") }
                },
                CacheParameters = new Dictionary<string, ICacheParameter>()
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Description of foo LISP template",
                Identity = "foo.test.Lisp",
                GroupIdentity = "foo.test.template",
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "language", ResolutionTestHelper.CreateTestCacheTag("LISP") }
                },
                CacheParameters = new Dictionary<string, ICacheParameter>()
            });

            INewCommandInput userInputs = new MockNewCommandInput()
            {
                TemplateName = "foo"
            };

            TemplateResolutionResult matchResult = TemplateResolver.GetTemplateResolutionResult(templatesToSearch, new MockHostSpecificDataLoader(), userInputs, "Perl");
            Assert.True(matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyCollection<ITemplateMatchInfo> unambiguousGroup));
            Assert.Equal(2, unambiguousGroup.Count);
            Assert.True(matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo templateToInvoke, out TemplateResolutionResult.SingleInvokableMatchStatus resultStatus));
            Assert.Equal("foo.test.Perl", templateToInvoke.Info.Identity);
            Assert.Equal(TemplateResolutionResult.SingleInvokableMatchStatus.SingleMatch, resultStatus);
        }

        [Fact(DisplayName = nameof(TestPerformCoreTemplateQuery_InputLanguageIsPreferredOverDefault))]
        public void TestPerformCoreTemplateQuery_InputLanguageIsPreferredOverDefault()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Description of foo Perl template",
                Identity = "foo.test.Perl",
                GroupIdentity = "foo.test.template",
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "language", ResolutionTestHelper.CreateTestCacheTag("Perl") }
                },
                CacheParameters = new Dictionary<string, ICacheParameter>()
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Description of foo LISP template",
                Identity = "foo.test.Lisp",
                GroupIdentity = "foo.test.template",
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "language", ResolutionTestHelper.CreateTestCacheTag("LISP") }
                },
                CacheParameters = new Dictionary<string, ICacheParameter>()
            });

            INewCommandInput userInputs = new MockNewCommandInput()
            {
                TemplateName = "foo",
                Language = "LISP"
            };

            TemplateResolutionResult matchResult = TemplateResolver.GetTemplateResolutionResult(templatesToSearch, new MockHostSpecificDataLoader(), userInputs, "Perl");
            Assert.True(matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyCollection<ITemplateMatchInfo> unambiguousGroup));
            Assert.Equal(1, unambiguousGroup.Count);
            Assert.Equal("foo.test.Lisp", unambiguousGroup.First().Info.Identity);
            Assert.True(matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo templateToInvoke, out TemplateResolutionResult.SingleInvokableMatchStatus resultStatus));
            Assert.Equal("foo.test.Lisp", templateToInvoke.Info.Identity);
            Assert.Equal(TemplateResolutionResult.SingleInvokableMatchStatus.SingleMatch, resultStatus);
        }

        [Fact(DisplayName = nameof(TestPerformCoreTemplateQuery_GroupIsFound))]
        public void TestPerformCoreTemplateQuery_GroupIsFound()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template old",
                Identity = "foo.test.old",
                GroupIdentity = "foo.test.template",
                Precedence = 100,
                CacheParameters = new Dictionary<string, ICacheParameter>(),
                Tags = new Dictionary<string, ICacheTag>()
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template new",
                Identity = "foo.test.new",
                GroupIdentity = "foo.test.template",
                Precedence = 200,
                CacheParameters = new Dictionary<string, ICacheParameter>(),
                Tags = new Dictionary<string, ICacheTag>()
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "bar",
                Name = "Bar template",
                Identity = "bar.test",
                GroupIdentity = "bar.test.template",
                Precedence = 100,
                CacheParameters = new Dictionary<string, ICacheParameter>(),
                Tags = new Dictionary<string, ICacheTag>()
            });

            INewCommandInput userInputs = new MockNewCommandInput()
            {
                TemplateName = "foo"
            };

            TemplateResolutionResult matchResult = TemplateResolver.GetTemplateResolutionResult(templatesToSearch, new MockHostSpecificDataLoader(), userInputs, null);
            Assert.True(matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyCollection<ITemplateMatchInfo> unambiguousGroup));
            Assert.Equal(2, unambiguousGroup.Count);
            Assert.Contains(unambiguousGroup, x => string.Equals(x.Info.Identity, "foo.test.old"));
            Assert.Contains(unambiguousGroup, x => string.Equals(x.Info.Identity, "foo.test.new"));
            Assert.True(matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo templateToInvoke, out TemplateResolutionResult.SingleInvokableMatchStatus resultStatus));
            Assert.Equal(TemplateResolutionResult.SingleInvokableMatchStatus.SingleMatch, resultStatus);
            Assert.Equal("foo.test.new", templateToInvoke.Info.Identity);
        }

        [Fact(DisplayName = nameof(TestPerformCoreTemplateQuery_ParameterNameDisambiguates))]
        public void TestPerformCoreTemplateQuery_ParameterNameDisambiguates()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test.old",
                GroupIdentity = "foo.test.template",
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase),
                CacheParameters = new Dictionary<string, ICacheParameter>(StringComparer.OrdinalIgnoreCase)
                {
                    { "bar", new CacheParameter() },
                }
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test.new",
                GroupIdentity = "foo.test.template",
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase),
                CacheParameters = new Dictionary<string, ICacheParameter>(StringComparer.OrdinalIgnoreCase)
                {
                    { "baz", new CacheParameter() },
                }
            });

            INewCommandInput userInputs = new MockNewCommandInput(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "baz", "whatever" }
                }
            )
            {
                TemplateName = "foo"
            };

            TemplateResolutionResult matchResult = TemplateResolver.GetTemplateResolutionResult(templatesToSearch, new MockHostSpecificDataLoader(), userInputs, null);
            Assert.True(matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyCollection<ITemplateMatchInfo> unambiguousGroup));
            Assert.Equal(2, unambiguousGroup.Count);
            Assert.True(matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo templateToInvoke, out TemplateResolutionResult.SingleInvokableMatchStatus resultStatus));
            Assert.Equal("foo.test.new", templateToInvoke.Info.Identity);
            Assert.Equal(TemplateResolutionResult.SingleInvokableMatchStatus.SingleMatch, resultStatus);
        }

        [Fact(DisplayName = nameof(TestPerformCoreTemplateQuery_ParameterValueDisambiguates))]
        public void TestPerformCoreTemplateQuery_ParameterValueDisambiguates()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template old",
                Identity = "foo.test.old",
                GroupIdentity = "foo.test.template",
                Precedence = 100,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "framework", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "netcoreapp1.0", "netcoreapp1.1" }) }
                },
                CacheParameters = new Dictionary<string, ICacheParameter>()
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template new",
                Identity = "foo.test.new",
                GroupIdentity = "foo.test.template",
                Precedence = 200,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "framework", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "netcoreapp2.0" }) }
                },
                CacheParameters = new Dictionary<string, ICacheParameter>()
            });

            INewCommandInput userInputs = new MockNewCommandInput(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "framework", "netcoreapp1.0" }
                }
            )
            {
                TemplateName = "foo"
            };

            TemplateResolutionResult matchResult = TemplateResolver.GetTemplateResolutionResult(templatesToSearch, new MockHostSpecificDataLoader(), userInputs, null);
            Assert.True(matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyCollection<ITemplateMatchInfo> unambiguousGroup));
            Assert.Equal(2, unambiguousGroup.Count);
            Assert.True(matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo templateToInvoke, out TemplateResolutionResult.SingleInvokableMatchStatus resultStatus));
            Assert.Equal("foo.test.old", templateToInvoke.Info.Identity);
            Assert.Equal(TemplateResolutionResult.SingleInvokableMatchStatus.SingleMatch, resultStatus);
        }

        [Fact(DisplayName = nameof(TestPerformCoreTemplateQuery_UnknownParameterNameInvalidatesMatch))]
        public void TestPerformCoreTemplateQuery_UnknownParameterNameInvalidatesMatch()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test",
                GroupIdentity = "foo.test.template",
                Precedence = 100,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase),
                CacheParameters = new Dictionary<string, ICacheParameter>()
                {
                    { "bar", new CacheParameter() },
                }
            });

            INewCommandInput userInputs = new MockNewCommandInput(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "baz", null }
                }
            )
            {
                TemplateName = "foo"
            };

            IHostSpecificDataLoader hostSpecificDataLoader = new MockHostSpecificDataLoader();

            TemplateResolutionResult matchResult = TemplateResolver.GetTemplateResolutionResult(templatesToSearch, hostSpecificDataLoader, userInputs, null);
            Assert.True(matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyCollection<ITemplateMatchInfo> unambiguousGroup));
            Assert.Equal(1, unambiguousGroup.Count);
            Assert.False(matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo templateToInvoke, out TemplateResolutionResult.SingleInvokableMatchStatus resultStatus));
            Assert.Null(templateToInvoke);
            Assert.Equal(TemplateResolutionResult.SingleInvokableMatchStatus.InvalidParameter, resultStatus);

            var invalidParams = unambiguousGroup.First().GetInvalidParameterNames();
            Assert.Equal(1, invalidParams.Count);
            Assert.Equal("baz", invalidParams[0]);
        }

        [Fact(DisplayName = nameof(TestPerformCoreTemplateQuery_InvalidChoiceValueInvalidatesMatch))]
        public void TestPerformCoreTemplateQuery_InvalidChoiceValueInvalidatesMatch()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test.1x",
                GroupIdentity = "foo.test.template",
                Precedence = 100,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "framework", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "netcoreapp1.0", "netcoreapp1.1" }) }
                },
                CacheParameters = new Dictionary<string, ICacheParameter>()
            });
            templatesToSearch.Add(new TemplateInfo()
            {
                ShortName = "foo",
                Name = "Foo template",
                Identity = "foo.test.2x",
                GroupIdentity = "foo.test.template",
                Precedence = 200,
                Tags = new Dictionary<string, ICacheTag>(StringComparer.OrdinalIgnoreCase)
                {
                    { "framework", ResolutionTestHelper.CreateTestCacheTag(new List<string>() { "netcoreapp2.0" }) }
                },
                CacheParameters = new Dictionary<string, ICacheParameter>()
            });

            INewCommandInput userInputs = new MockNewCommandInput(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "framework", "netcoreapp3.0" }
                }
            )
            {
                TemplateName = "foo"
            };

            IHostSpecificDataLoader hostSpecificDataLoader = new MockHostSpecificDataLoader();
            TemplateResolutionResult matchResult = TemplateResolver.GetTemplateResolutionResult(templatesToSearch, hostSpecificDataLoader, userInputs, null);
            Assert.True(matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyCollection<ITemplateMatchInfo> unambiguousGroup));
            Assert.Equal(2, unambiguousGroup.Count);
            Assert.False(matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo templateToInvoke, out TemplateResolutionResult.SingleInvokableMatchStatus resultStatus));
            Assert.Null(templateToInvoke);
            Assert.Equal(TemplateResolutionResult.SingleInvokableMatchStatus.InvalidParameter, resultStatus);

            Assert.Contains(unambiguousGroup.ElementAt(0).MatchDisposition, x => x.Kind == MatchKind.InvalidParameterValue);
            Assert.Contains(unambiguousGroup.ElementAt(1).MatchDisposition, x => x.Kind == MatchKind.InvalidParameterValue);
        }
    }
}
