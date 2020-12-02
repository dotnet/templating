// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.TemplateResolution;
using Microsoft.TemplateEngine.Cli.UnitTests.CliMocks;
using Microsoft.TemplateEngine.Cli.UnitTests.CliMocks.XUnit;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;
using Xunit;
using static Microsoft.TemplateEngine.Cli.TemplateResolution.TemplateResolutionResult;

namespace Microsoft.TemplateEngine.Cli.UnitTests.TemplateResolutionTests
{
    public class TemplateResolverTests
    {
        //data contents:
        //command
        //templates to use
        //default language
        //unambiguous group status
        //expected template identities
        internal class TemplateResolution_UnambiguousGroup_TestData : TheoryData<XUnitMockNewCommandInput, XUnitMockTemplateInfo[], string, int, string[]>
        {
            public TemplateResolution_UnambiguousGroup_TestData()
            {
                var templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("Template1", identity: "Template1"),
                    new XUnitMockTemplateInfo("Template2", identity: "Template2")
                };
                Add(new XUnitMockNewCommandInput("Template2"), templates, null, (int)UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "Template2" });
                Add(new XUnitMockNewCommandInput("Template3"), templates, null, (int)UnambiguousTemplateGroupStatus.NoMatch, new string[] {});
                Add(new XUnitMockNewCommandInput("Template"), templates, null, (int)UnambiguousTemplateGroupStatus.Ambiguous, new string[] {});

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("ShortName1", identity: "Template1", groupIdentity:"Group", precedence:100),
                    new XUnitMockTemplateInfo("ShortName2", identity: "Template2", groupIdentity:"Group", precedence:200)
                };
                Add(new XUnitMockNewCommandInput("ShortName1"), templates, null, (int)UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "Template1", "Template2" });
                Add(new XUnitMockNewCommandInput("ShortName2"), templates, null, (int)UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "Template1", "Template2" });
                Add(new XUnitMockNewCommandInput("ShortName"), templates, null, (int)UnambiguousTemplateGroupStatus.SingleMatch,  new string[] { "Template1", "Template2" });

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("ShortName1", identity: "Template1", groupIdentity:"Group"),
                    new XUnitMockTemplateInfo("ShortName2", identity: "Template2", groupIdentity:"Group")
                };
                Add(new XUnitMockNewCommandInput("ShortName1"), templates, null, (int)UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "Template1", "Template2" });
                Add(new XUnitMockNewCommandInput("ShortName2"), templates, null, (int)UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "Template1", "Template2" });
                Add(new XUnitMockNewCommandInput("ShortName"), templates, null, (int)UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "Template1", "Template2" });


                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.Perl", groupIdentity: "foo.group").WithTag("language", "Perl"),
                    new XUnitMockTemplateInfo("foo", identity: "foo.Lisp", groupIdentity: "foo.group").WithTag("language", "LISP")
                };
                Add(new XUnitMockNewCommandInput("foo"), templates, "Perl", (int)UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.Perl", "foo.Lisp" });
                Add(new XUnitMockNewCommandInput("foo", language: "LISP"), templates, null, (int)UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.Lisp" });

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.100", groupIdentity: "foo.group", precedence: 100),
                    new XUnitMockTemplateInfo("foo", identity: "foo.200", groupIdentity: "foo.group", precedence: 200),
                    new XUnitMockTemplateInfo("bar", identity: "bar.200", groupIdentity: "bar.group", precedence: 200),
                };

                Add(new XUnitMockNewCommandInput("foo"), templates, null, (int)UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.100", "foo.200" });

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.bar", groupIdentity: "foo.group").WithParameters("bar"),
                    new XUnitMockTemplateInfo("foo", identity: "foo.baz", groupIdentity: "foo.group").WithParameters("baz"),
                };
                Add(new XUnitMockNewCommandInput("foo").WithOption("baz", "whatever"), templates, null, (int)UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.bar", "foo.baz" });

                Add(new XUnitMockNewCommandInput("foo").WithOption("bat", "whatever"), templates, null, (int)UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.bar", "foo.baz" });

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group").WithTag("framework", "netcoreapp2.1", "netcoreapp3.1"),
                    new XUnitMockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group").WithTag("framework", "net5.0"),
                };

                Add(new XUnitMockNewCommandInput("foo").WithOption("framework", "net5.0"), templates, null, (int)UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.1", "foo.2" });
                Add(new XUnitMockNewCommandInput("foo").WithOption("framework", "netcoreapp2.0"), templates, null, (int)UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.1", "foo.2" });

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group").WithTag("MyChoice", "value_1_example"),
                    new XUnitMockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group").WithTag("MyChoice", "value_2_example", "value_3_example"),
                };

                Add(new XUnitMockNewCommandInput("foo").WithOption("MyChoice", "value_"), templates, null, (int)UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.1", "foo.2" });
                Add(new XUnitMockNewCommandInput("foo").WithOption("MyChoice", "value_1"), templates, null, (int)UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.1", "foo.2" });

            }
        }
        [Theory(DisplayName = nameof(TemplateResolution_UnambiguousGroup_Test))]
        [ClassData(typeof(TemplateResolution_UnambiguousGroup_TestData))]
        internal void TemplateResolution_UnambiguousGroup_Test(XUnitMockNewCommandInput command, XUnitMockTemplateInfo[] templateSet, string defaultLanguage,  int expectedStatus, string[] expectedIdentities)
        {
            var matchResult = TemplateResolver.GetTemplateResolutionResult(templateSet, new MockHostSpecificDataLoader(), command, defaultLanguage);

            Assert.Equal(expectedStatus, (int) matchResult.UnambigiousTemplateGroupCheckStatus);

            //compatibility with old methods
            Assert.Equal(expectedStatus ==  (int)UnambiguousTemplateGroupStatus.SingleMatch,
                matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyCollection<ITemplateMatchInfo> unambiguousGroup));

            if (expectedStatus == (int)UnambiguousTemplateGroupStatus.SingleMatch)
            {
                var identities = matchResult.UnambiguousTemplateGroup.Templates.Select(t => t.Info.Identity);
                Assert.Equal(expectedIdentities.Length, identities.Count());
                foreach (string identity in expectedIdentities)
                {
                    Assert.Single(identities.Where(i => i == identity));
                }
            }
            else
            {
                Assert.Null(matchResult.UnambiguousTemplateGroup);
                Assert.Null(unambiguousGroup);
            }
        }

        //data contents:
        //command
        //templates to use
        //default language
        //resolution status
        //expected template identity
        internal class TemplateResolution_TemplateToInvoke_TestData : TheoryData<XUnitMockNewCommandInput, XUnitMockTemplateInfo[], string, int, string>
        {
            public TemplateResolution_TemplateToInvoke_TestData()
            {
                var templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("Template1", identity: "Template1"),
                    new XUnitMockTemplateInfo("Template2", identity: "Template2")
                };
                Add(new XUnitMockNewCommandInput("Template2"), templates, null, (int)SingleInvokableMatchStatus.SingleMatch, "Template2");
                Add(new XUnitMockNewCommandInput("Template3"), templates, null, (int)SingleInvokableMatchStatus.NoMatch, null);
                Add(new XUnitMockNewCommandInput("Template"), templates, null, (int)SingleInvokableMatchStatus.AmbiguousTemplateGroupChoice, null);

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("Template", identity: "Template1"),
                    new XUnitMockTemplateInfo("Template", identity: "Template2")
                };
                Add(new XUnitMockNewCommandInput("Template"), templates, null, (int)SingleInvokableMatchStatus.AmbiguousTemplateGroupChoice, null);

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("ShortName1", identity: "Template1", groupIdentity:"Group", precedence:100),
                    new XUnitMockTemplateInfo("ShortName2", identity: "Template2", groupIdentity:"Group", precedence:200)
                };
                Add(new XUnitMockNewCommandInput("ShortName1"), templates, null, (int)SingleInvokableMatchStatus.SingleMatch, "Template2");
                Add(new XUnitMockNewCommandInput("ShortName2"), templates, null, (int)SingleInvokableMatchStatus.SingleMatch, "Template2");
                Add(new XUnitMockNewCommandInput("ShortName"), templates, null, (int)SingleInvokableMatchStatus.SingleMatch, "Template2");

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("ShortName1", identity: "Template1", groupIdentity:"Group"),
                    new XUnitMockTemplateInfo("ShortName2", identity: "Template2", groupIdentity:"Group")
                };
                Add(new XUnitMockNewCommandInput("ShortName1"), templates, null, (int)SingleInvokableMatchStatus.AmbiguousTemplateChoice, null);
                Add(new XUnitMockNewCommandInput("ShortName2"), templates, null, (int)SingleInvokableMatchStatus.AmbiguousTemplateChoice, null);
                Add(new XUnitMockNewCommandInput("ShortName"), templates, null, (int)SingleInvokableMatchStatus.AmbiguousTemplateChoice, null);

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.Perl", groupIdentity: "foo.group").WithTag("language", "Perl"),
                    new XUnitMockTemplateInfo("foo", identity: "foo.Lisp", groupIdentity: "foo.group").WithTag("language", "LISP")
                };
                Add(new XUnitMockNewCommandInput("foo"), templates, "Perl", (int)SingleInvokableMatchStatus.SingleMatch, "foo.Perl");
                Add(new XUnitMockNewCommandInput("foo", language: "LISP"), templates, null, (int)SingleInvokableMatchStatus.SingleMatch, "foo.Lisp");

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.100", groupIdentity: "foo.group", precedence: 100),
                    new XUnitMockTemplateInfo("foo", identity: "foo.200", groupIdentity: "foo.group", precedence: 200),
                    new XUnitMockTemplateInfo("bar", identity: "bar.200", groupIdentity: "bar.group", precedence: 200),
                };

                Add(new XUnitMockNewCommandInput("foo"), templates, null, (int)SingleInvokableMatchStatus.SingleMatch, "foo.200");

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.bar", groupIdentity: "foo.group").WithParameters("bar"),
                    new XUnitMockTemplateInfo("foo", identity: "foo.baz", groupIdentity: "foo.group").WithParameters("baz"),
                };
                Add(new XUnitMockNewCommandInput("foo").WithOption("baz", "whatever"), templates, null, (int)SingleInvokableMatchStatus.SingleMatch, "foo.baz");

                Add(new XUnitMockNewCommandInput("foo").WithOption("bat", "whatever"), templates, null, (int)SingleInvokableMatchStatus.InvalidParameter, null);

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group").WithTag("framework", "netcoreapp2.1", "netcoreapp3.1"),
                    new XUnitMockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group").WithTag("framework", "net5.0"),
                };

                Add(new XUnitMockNewCommandInput("foo").WithOption("framework", "net5.0"), templates, null, (int)SingleInvokableMatchStatus.SingleMatch, "foo.2");
                Add(new XUnitMockNewCommandInput("foo").WithOption("framework", "netcoreapp2.0"), templates, null, (int)SingleInvokableMatchStatus.InvalidParameter, null);

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group", precedence: 100).WithTag("MyChoice", "value_1"),
                    new XUnitMockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group", precedence: 200).WithTag("MyChoice", "value_2"),
                };

                Add(new XUnitMockNewCommandInput("foo").WithOption("MyChoice", "value_"), templates, null, (int)SingleInvokableMatchStatus.AmbiguousParameterValueChoice, null);
                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group", precedence: 100).WithTag("MyChoice", "value_1"),
                    new XUnitMockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group", precedence: 200).WithTag("MyChoice", "value_2", "value_3"),
                };
                Add(new XUnitMockNewCommandInput("foo").WithOption("MyChoice", "value_"), templates, null, (int)SingleInvokableMatchStatus.AmbiguousParameterValueChoice, null);

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group", precedence: 100).WithTag("MyChoice", "value_1", "value_2"),
                    new XUnitMockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group", precedence: 200).WithTag("MyChoice", "value_3", "value_4"),
                };
                Add(new XUnitMockNewCommandInput("foo").WithOption("MyChoice", "value_"), templates, null, (int)SingleInvokableMatchStatus.AmbiguousParameterValueChoice, null);

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group", precedence: 100)
                        .WithTag("MyChoice", "value_1", "other_value")
                        .WithTag("OtherChoice", "foo_"),

                    new XUnitMockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group", precedence: 200)
                        .WithTag("MyChoice", "value_")
                        .WithTag("OtherChoice", "foo_", "bar_1"),
                };
                Add(new XUnitMockNewCommandInput("foo").WithOption("MyChoice", "value_").WithOption("OtherChoice", "foo_"), templates, null, (int)SingleInvokableMatchStatus.SingleMatch, "foo.2");

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.1.FSharp", groupIdentity: "foo.group", precedence: 100)
                        .WithTag("language", "F#")
                };
                Add(new XUnitMockNewCommandInput("foo"), templates, null, (int)SingleInvokableMatchStatus.SingleMatch, "foo.1.FSharp");
                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.1.FSharp", groupIdentity: "foo.group", precedence: 100)
                        .WithTag("language", "F#"),
                    new XUnitMockTemplateInfo("foo", identity: "foo.1.VB", groupIdentity: "foo.group", precedence: 200)
                        .WithTag("language", "VB")
                };
                Add(new XUnitMockNewCommandInput("foo"), templates, null, (int)SingleInvokableMatchStatus.SingleMatch, "foo.1.VB");

                templates = new XUnitMockTemplateInfo[]
                {
                    new XUnitMockTemplateInfo("foo", identity: "foo.1.FSharp", groupIdentity: "foo.group", precedence: 200)
                        .WithTag("language", "F#"),
                    new XUnitMockTemplateInfo("foo", identity: "foo.1.VB", groupIdentity: "foo.group", precedence: 200)
                        .WithTag("language", "VB")
                };
                Add(new XUnitMockNewCommandInput("foo"), templates, null, (int)SingleInvokableMatchStatus.AmbiguousTemplateChoice, null);
            }
        }

        [Theory(DisplayName = nameof(TemplateResolution_TemplateToInvoke_Test))]
        [ClassData(typeof(TemplateResolution_TemplateToInvoke_TestData))]
        internal void TemplateResolution_TemplateToInvoke_Test(XUnitMockNewCommandInput command, XUnitMockTemplateInfo[] templateSet, string defaultLanguage, int expectedStatus, string expectedIdentity)
        {
            TemplateResolutionResult matchResult = TemplateResolver.GetTemplateResolutionResult(templateSet, new MockHostSpecificDataLoader(), command, defaultLanguage);

            Assert.Equal(expectedStatus, (int)matchResult.Status);

            //compatibility with old methods
            Assert.Equal(expectedStatus == (int)SingleInvokableMatchStatus.SingleMatch,
                matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo templateToInvoke, out SingleInvokableMatchStatus tryStatus));
            Assert.Equal(expectedStatus, (int)tryStatus);

            if (expectedStatus == (int)SingleInvokableMatchStatus.SingleMatch)
            {
                Assert.Equal(expectedIdentity, matchResult.TemplateToInvoke.Info.Identity);
                Assert.Equal(expectedIdentity, templateToInvoke.Info.Identity);
            }
            else
            {
                Assert.Null(matchResult.TemplateToInvoke);
                Assert.Null(templateToInvoke);
            }
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


    }
}
