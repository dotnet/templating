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
using Microsoft.TemplateEngine.Mocks;
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
        [Fact(DisplayName = nameof(TestFindHighestPrecedenceTemplateIfAllSameGroupIdentity))]
        public void TestFindHighestPrecedenceTemplateIfAllSameGroupIdentity()
        {
            List<ITemplateMatchInfo> templatesToCheck = new List<ITemplateMatchInfo>();

            templatesToCheck.Add(new TemplateMatchInfo(
                new MockTemplateInfo("Template1", name: "Template1", identity: "Template1", groupIdentity: "TestGroup", precedence: 10), null));
            templatesToCheck.Add(new TemplateMatchInfo(
                new MockTemplateInfo("Template2", name: "Template2", identity: "Template2", groupIdentity: "TestGroup", precedence: 20), null));
            templatesToCheck.Add(new TemplateMatchInfo(
                new MockTemplateInfo("Template3", name: "Template3", identity: "Template3", groupIdentity: "TestGroup", precedence: 0), null));

            ITemplateMatchInfo highestPrecedenceTemplate = TemplateResolver.FindHighestPrecedenceTemplateIfAllSameGroupIdentity(templatesToCheck);
            Assert.NotNull(highestPrecedenceTemplate);
            Assert.Equal("Template2", highestPrecedenceTemplate.Info.Identity);
            Assert.Equal(20, highestPrecedenceTemplate.Info.Precedence);
        }

        [Fact(DisplayName = nameof(TestFindHighestPrecedenceTemplateIfAllSameGroupIdentity_ReturnsNullIfGroupsAreDifferent))]
        public void TestFindHighestPrecedenceTemplateIfAllSameGroupIdentity_ReturnsNullIfGroupsAreDifferent()
        {
            List<ITemplateMatchInfo> templatesToCheck = new List<ITemplateMatchInfo>();
            templatesToCheck.Add(new TemplateMatchInfo(
                new MockTemplateInfo("Template1", name: "Template1", identity: "Template1", groupIdentity: "TestGroup", precedence: 10), null));
            templatesToCheck.Add(new TemplateMatchInfo(
                new MockTemplateInfo("Template2", name: "Template2", identity: "Template2", groupIdentity: "RealGroup", precedence: 20), null));

            ITemplateMatchInfo highestPrecedenceTemplate = TemplateResolver.FindHighestPrecedenceTemplateIfAllSameGroupIdentity(templatesToCheck);
            Assert.Null(highestPrecedenceTemplate);
        }

        [Fact(DisplayName = nameof(TestPerformAllTemplatesInContextQuery))]
        public void TestPerformAllTemplatesInContextQuery()
        {
            List<ITemplateInfo> templatesToSearch = new List<ITemplateInfo>();
            templatesToSearch.Add(new MockTemplateInfo("Template1", name: "Template1", identity: "Template1")
                                    .WithTag("type", "project"));
            templatesToSearch.Add(new MockTemplateInfo("Template2", name: "Template2", identity: "Template2")
                                    .WithTag("type", "item"));
            templatesToSearch.Add(new MockTemplateInfo("Template3", name: "Template3", identity: "Template3")
                                    .WithTag("type", "myType"));
            templatesToSearch.Add(new MockTemplateInfo("Template4", name: "Template4", identity: "Template4")
                                    .WithTag("type", "project"));
            templatesToSearch.Add(new MockTemplateInfo("Template5", name: "Template5", identity: "Template5")
                                     .WithTag("type", "project"));

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

        public static IEnumerable<object[]> Get_TemplateResolution_UnambiguousGroup_TestData()
        {
            //TestPerformCoreTemplateQuery_UniqueNameMatchesCorrectly
            yield return new object[]
            {
                new MockNewCommandInput("Template2"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("Template1", name: "Long name of Template1", identity: "Template1"),
                    new MockTemplateInfo("Template2", name: "Long name of Template2", identity: "Template2"),
                },
                null,
                TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch,
                new string [] { "Template2" }
            };

            //TestPerformCoreTemplateQuery_InputLanguageIsPreferredOverDefault
            yield return new object[]
            {
                new MockNewCommandInput("foo", "LISP"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Description of foo Perl template", identity: "foo.test.Perl", groupIdentity: "foo.test.template")
                                      .WithTag("language", "Perl"),
                    new MockTemplateInfo("foo", name: "Description of foo LISP template", identity: "foo.test.Lisp", groupIdentity: "foo.test.template")
                                      .WithTag("language", "LISP"),
                },
                "Perl",
                TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch,
                new string [] { "foo.test.Lisp" }
            };

            //TestPerformCoreTemplateQuery_GroupIsFound
            yield return new object[]
            {
                new MockNewCommandInput("foo"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template old", identity: "foo.test.old", groupIdentity: "foo.test.template", precedence: 100),
                    new MockTemplateInfo("foo", name: "Foo template new", identity: "foo.test.new", groupIdentity: "foo.test.template", precedence: 200),
                    new MockTemplateInfo("bar", name: "Bar template", identity: "bar.test", groupIdentity: "bar.test.template", precedence: 100)
                },
                null,
                TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch,
                new string [] { "foo.test.old", "foo.test.new" }
            };

            //TestPerformCoreTemplateQuery_ParameterNameDisambiguates
            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("baz", "whatever"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template old", identity: "foo.test.old", groupIdentity: "foo.test.template").WithParameters("bar"),
                    new MockTemplateInfo("foo", name: "Foo template new", identity: "foo.test.new", groupIdentity: "foo.test.template").WithParameters("baz")
                },
                null,
                TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch,
                new string [] { "foo.test.new", "foo.test.old" }
            };

            //TestPerformCoreTemplateQuery_ParameterValueDisambiguates
            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("framework", "netcoreapp2.1"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template old", identity: "foo.test.old", groupIdentity: "foo.test.template", precedence: 100).WithTag("framework", "netcoreapp3.1", "netcoreapp2.1"),
                    new MockTemplateInfo("foo", name: "Foo template new", identity: "foo.test.new", groupIdentity: "foo.test.template", precedence: 200).WithTag("framework", "net5.0")
                },
                null,
                TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch,
                new string [] { "foo.test.old", "foo.test.new" }
            };

            //TestPerformCoreTemplateQuery_UnknownParameterNameInvalidatesMatch
            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("baz"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test", groupIdentity: "foo.test.template", precedence: 100).WithParameters("bar"),
                },
                null,
                TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch,
                new string [] { "foo.test" }
            };

            //TestPerformCoreTemplateQuery_InvalidChoiceValueInvalidatesMatch
            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("framework", "netcoreapp3.0"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test.1x", groupIdentity: "foo.test.template", precedence: 100).WithTag("framework", "netcoreapp2.1", "netcoreapp3.1"),
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test.2x", groupIdentity: "foo.test.template", precedence: 200).WithTag("framework", "net5.0")
                },
                null,
                TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch,
                new string [] { "foo.test.1x", "foo.test.2x" }
            };

            //SingularInvokableMatchTests
            //MultipleTemplatesInGroupHavingSingleStartsWithOnSameParamIsAmbiguous
            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("MyChoice", "value_"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1", groupIdentity: "foo.test.template", precedence: 100)
                                    .WithTag("MyChoice", "value_1"),
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_2", groupIdentity: "foo.test.template", precedence: 200)
                                    .WithTag("MyChoice", "value_2")
                },
                null,
                TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch,
                new string [] { "foo.test_1", "foo.test_2" }
            };

            //MultipleTemplatesInGroupParamPartiaMatch_TheOneHavingSingleStartsWithIsTheSingularInvokableMatch
            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("MyChoice", "value_"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1", groupIdentity: "foo.test.template", precedence: 100)
                                    .WithTag("MyChoice", "value_1"),
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_2", groupIdentity: "foo.test.template", precedence: 200)
                                    .WithTag("MyChoice", "value_2", "value_3")
                },
                null,
                TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch,
                new string [] { "foo.test_1", "foo.test_2" }
            };

            //MultipleTemplatesInGroupHavingAmbiguousParamMatchOnSameParamIsAmbiguous
            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("MyChoice", "value_"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1", groupIdentity: "foo.test.template", precedence: 100)
                                    .WithTag("MyChoice", "value_1", "value_2"),
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_2", groupIdentity: "foo.test.template", precedence: 200)
                                    .WithTag("MyChoice", "value_3", "value_4")
                },
                null,
                TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch,
                new string [] { "foo.test_1", "foo.test_2" }
            };

            //MultipleTemplatesInGroupHavingSingularStartMatchesOnDifferentParams_HighPrecedenceIsChosen
            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("MyChoice", "value_").WithTemplateOption("OtherChoice", "foo_"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1", groupIdentity: "foo.test.template", precedence: 100)
                                    .WithTag("MyChoice", "value_1", "other_value")
                                    .WithTag("OtherChoice", "foo_"),
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_2", groupIdentity: "foo.test.template", precedence: 200)
                                    .WithTag("MyChoice", "value_")
                                    .WithTag("OtherChoice", "foo_", "bar_1")
                },
                null,
                TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch,
                new string [] { "foo.test_1", "foo.test_2" }
            };

            //GivenOneInvokableTemplateWithNonDefaultLanguage_ItIsChosen
            yield return new object[]
            {
                new MockNewCommandInput("foo"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1", groupIdentity: "foo.test.template", precedence: 100)
                                    .WithTag("language", "F#")
                },
                null,
                TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch,
                new string [] { "foo.test_1" }
            };

            //GivenTwoInvokableTemplatesNonDefaultLanguage_HighPrecedenceIsChosen
            yield return new object[]
            {
                new MockNewCommandInput("foo"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1.FSharp", groupIdentity: "foo.test.template", precedence: 100)
                                     .WithTag("language", "F#"),
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1.VB", groupIdentity: "foo.test.template", precedence: 200)
                                     .WithTag("language", "VB")
                },
                null,
                TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch,
                new string [] { "foo.test_1.FSharp", "foo.test_1.VB" }
            };

            //GivenMultipleHighestPrecedenceTemplates_ResultIsAmbiguous
            yield return new object[]
            {
                new MockNewCommandInput("foo"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1.FSharp", groupIdentity: "foo.test.template", precedence: 100)
                                     .WithTag("language", "F#"),
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1.VB", groupIdentity: "foo.test.template", precedence: 100)
                                     .WithTag("language", "VB")
                },
                null,
                TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch,
                new string [] { "foo.test_1.FSharp", "foo.test_1.VB" }
            };

            //new tests
            var templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("Template1", identity: "Template1"),
                    new MockTemplateInfo("Template2", identity: "Template2")
            };
            yield return new object[] { new MockNewCommandInput("Template2"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "Template2" } };
            yield return new object[] { new MockNewCommandInput("Template3"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.NoMatch, new string[] { } };
            yield return new object[] { new MockNewCommandInput("Template"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.Ambiguous, new string[] { } };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("ShortName1", identity: "Template1", groupIdentity:"Group", precedence:100),
                    new MockTemplateInfo("ShortName2", identity: "Template2", groupIdentity:"Group", precedence:200)
            };
            yield return new object[] { new MockNewCommandInput("ShortName1"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "Template1", "Template2" } };
            yield return new object[] { new MockNewCommandInput("ShortName2"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "Template1", "Template2" } };
            yield return new object[] { new MockNewCommandInput("ShortName"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "Template1", "Template2" } };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("ShortName1", identity: "Template1", groupIdentity:"Group"),
                    new MockTemplateInfo("ShortName2", identity: "Template2", groupIdentity:"Group")
            };
            yield return new object[] { new MockNewCommandInput("ShortName1"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "Template1", "Template2" } };
            yield return new object[] { new MockNewCommandInput("ShortName2"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "Template1", "Template2" } };
            yield return new object[] { new MockNewCommandInput("ShortName"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "Template1", "Template2" } };


            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.Perl", groupIdentity: "foo.group").WithTag("language", "Perl"),
                    new MockTemplateInfo("foo", identity: "foo.Lisp", groupIdentity: "foo.group").WithTag("language", "LISP")
            };
            yield return new object[] { new MockNewCommandInput("foo"), templates, "Perl", (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.Perl", "foo.Lisp" } };
            yield return new object[] { new MockNewCommandInput("foo", language: "LISP"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.Lisp" } };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.100", groupIdentity: "foo.group", precedence: 100),
                    new MockTemplateInfo("foo", identity: "foo.200", groupIdentity: "foo.group", precedence: 200),
                    new MockTemplateInfo("bar", identity: "bar.200", groupIdentity: "bar.group", precedence: 200),
            };

            yield return new object[] { new MockNewCommandInput("foo"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.100", "foo.200" } };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.bar", groupIdentity: "foo.group").WithParameters("bar"),
                    new MockTemplateInfo("foo", identity: "foo.baz", groupIdentity: "foo.group").WithParameters("baz"),
            };
            yield return new object[] { new MockNewCommandInput("foo").WithTemplateOption("baz", "whatever"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.bar", "foo.baz" } };

            yield return new object[] { new MockNewCommandInput("foo").WithTemplateOption("bat", "whatever"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.bar", "foo.baz" } };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group").WithTag("framework", "netcoreapp2.1", "netcoreapp3.1"),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group").WithTag("framework", "net5.0"),
            };

            yield return new object[] { new MockNewCommandInput("foo").WithTemplateOption("framework", "net5.0"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.1", "foo.2" } };
            yield return new object[] { new MockNewCommandInput("foo").WithTemplateOption("framework", "netcoreapp2.0"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.1", "foo.2" } };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group").WithTag("MyChoice", "value_1_example"),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group").WithTag("MyChoice", "value_2_example", "value_3_example"),
            };

            yield return new object[] { new MockNewCommandInput("foo").WithTemplateOption("MyChoice", "value_"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.1", "foo.2" } };
            yield return new object[] { new MockNewCommandInput("foo").WithTemplateOption("MyChoice", "value_1"), templates, null, (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, new string[] { "foo.1", "foo.2" } };
        }

        [Theory(DisplayName = nameof(TemplateResolution_UnambiguousGroup_Test))]
        [MemberData(nameof(Get_TemplateResolution_UnambiguousGroup_TestData))]
        internal void TemplateResolution_UnambiguousGroup_Test(MockNewCommandInput command, MockTemplateInfo[] templateSet, string defaultLanguage, int expectedStatus, string[] expectedIdentities)
        {
            var matchResult = TemplateResolver.GetTemplateResolutionResult(templateSet, new MockHostSpecificDataLoader(), command, defaultLanguage);

            Assert.Equal(expectedStatus, (int)matchResult.GroupResolutionStatus);

            //compatibility with old methods
            Assert.Equal(expectedStatus == (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch, matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyList<ITemplateMatchInfo> unambiguousGroup));

            if (expectedStatus == (int)TemplateResolutionResult.UnambiguousTemplateGroupStatus.SingleMatch)
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

        public static IEnumerable<object[]> Get_TemplateResolution_TemplateToInvoke_TestData()
        {
            //SingularInvokableMatchTests
            //MultipleTemplatesInGroupHavingSingleStartsWithOnSameParamIsAmbiguous
            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("MyChoice", "value_"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1", groupIdentity: "foo.test.template", precedence: 100)
                                    .WithTag("MyChoice", "value_1"),
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_2", groupIdentity: "foo.test.template", precedence: 200)
                                    .WithTag("MyChoice", "value_2")
                },
                null,
                TemplateResolutionResult.Status.AmbiguousParameterValueChoice,
                null
            };

            //MultipleTemplatesInGroupParamPartiaMatch_TheOneHavingSingleStartsWithIsTheSingularInvokableMatch
            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("MyChoice", "value_"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1", groupIdentity: "foo.test.template", precedence: 100)
                                    .WithTag("MyChoice", "value_1"),
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_2", groupIdentity: "foo.test.template", precedence: 200)
                                    .WithTag("MyChoice", "value_2", "value_3")
                },
                null,
                TemplateResolutionResult.Status.AmbiguousParameterValueChoice,
                null
            };

            //MultipleTemplatesInGroupHavingAmbiguousParamMatchOnSameParamIsAmbiguous
            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("MyChoice", "value_"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1", groupIdentity: "foo.test.template", precedence: 100)
                                    .WithTag("MyChoice", "value_1", "value_2"),
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_2", groupIdentity: "foo.test.template", precedence: 200)
                                    .WithTag("MyChoice", "value_3", "value_4")
                },
                null,
                TemplateResolutionResult.Status.AmbiguousParameterValueChoice,
                null
            };

            //MultipleTemplatesInGroupHavingSingularStartMatchesOnDifferentParams_HighPrecedenceIsChosen
            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("MyChoice", "value_").WithTemplateOption("OtherChoice", "foo_"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1", groupIdentity: "foo.test.template", precedence: 100)
                                    .WithTag("MyChoice", "value_1", "other_value")
                                    .WithTag("OtherChoice", "foo_"),
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_2", groupIdentity: "foo.test.template", precedence: 200)
                                    .WithTag("MyChoice", "value_")
                                    .WithTag("OtherChoice", "foo_", "bar_1")
                },
                null,
                TemplateResolutionResult.Status.SingleMatch,
                "foo.test_2"
            };

            //GivenOneInvokableTemplateWithNonDefaultLanguage_ItIsChosen
            yield return new object[]
            {
                new MockNewCommandInput("foo"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1", groupIdentity: "foo.test.template", precedence: 100)
                                    .WithTag("language", "F#")
                },
                null,
                TemplateResolutionResult.Status.SingleMatch,
                "foo.test_1"
            };

            //GivenTwoInvokableTemplatesNonDefaultLanguage_HighPrecedenceIsChosen
            yield return new object[]
            {
                new MockNewCommandInput("foo"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1.FSharp", groupIdentity: "foo.test.template", precedence: 100)
                                     .WithTag("language", "F#"),
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1.VB", groupIdentity: "foo.test.template", precedence: 200)
                                     .WithTag("language", "VB")
                },
                null,
                TemplateResolutionResult.Status.SingleMatch,
                "foo.test_1.VB"
            };

            //GivenMultipleHighestPrecedenceTemplates_ResultIsAmbiguous
            yield return new object[]
            {
                new MockNewCommandInput("foo"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1.FSharp", groupIdentity: "foo.test.template", precedence: 100)
                                     .WithTag("language", "F#"),
                    new MockTemplateInfo("foo", name: "Foo template", identity: "foo.test_1.VB", groupIdentity: "foo.test.template", precedence: 100)
                                     .WithTag("language", "VB")
                },
                null,
                TemplateResolutionResult.Status.AmbiguousLanguageChoice,
                null
            };

            //new tests
            var templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("Template1", identity: "Template1"),
                    new MockTemplateInfo("Template2", identity: "Template2")
            };
            yield return new object[] { new MockNewCommandInput("Template2"), templates, null, (int)TemplateResolutionResult.Status.SingleMatch, "Template2" };
            yield return new object[] { new MockNewCommandInput("Template3"), templates, null, (int)TemplateResolutionResult.Status.NoMatch, null };
            yield return new object[] { new MockNewCommandInput("Template"), templates, null, (int)TemplateResolutionResult.Status.AmbiguousTemplateGroupChoice, null };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("Template", identity: "Template1"),
                    new MockTemplateInfo("Template", identity: "Template2")
            };
            yield return new object[] { new MockNewCommandInput("Template"), templates, null, (int)TemplateResolutionResult.Status.AmbiguousTemplateGroupChoice, null };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("ShortName1", identity: "Template1", groupIdentity:"Group", precedence:100),
                    new MockTemplateInfo("ShortName2", identity: "Template2", groupIdentity:"Group", precedence:200)
            };
            yield return new object[] { new MockNewCommandInput("ShortName1"), templates, null, (int)TemplateResolutionResult.Status.SingleMatch, "Template2" };
            yield return new object[] { new MockNewCommandInput("ShortName2"), templates, null, (int)TemplateResolutionResult.Status.SingleMatch, "Template2" };
            yield return new object[] { new MockNewCommandInput("ShortName"), templates, null, (int)TemplateResolutionResult.Status.SingleMatch, "Template2" };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("ShortName1", identity: "Template1", groupIdentity:"Group"),
                    new MockTemplateInfo("ShortName2", identity: "Template2", groupIdentity:"Group")
            };
            yield return new object[] { new MockNewCommandInput("ShortName1"), templates, null, (int)TemplateResolutionResult.Status.AmbiguousTemplateChoice, null };
            yield return new object[] { new MockNewCommandInput("ShortName2"), templates, null, (int)TemplateResolutionResult.Status.AmbiguousTemplateChoice, null };
            yield return new object[] { new MockNewCommandInput("ShortName"), templates, null, (int)TemplateResolutionResult.Status.AmbiguousTemplateChoice, null };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.Perl", groupIdentity: "foo.group").WithTag("language", "Perl"),
                    new MockTemplateInfo("foo", identity: "foo.Lisp", groupIdentity: "foo.group").WithTag("language", "LISP")
            };
            yield return new object[] { new MockNewCommandInput("foo"), templates, "Perl", (int)TemplateResolutionResult.Status.SingleMatch, "foo.Perl" };
            yield return new object[] { new MockNewCommandInput("foo", language: "LISP"), templates, null, (int)TemplateResolutionResult.Status.SingleMatch, "foo.Lisp" };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.100", groupIdentity: "foo.group", precedence: 100),
                    new MockTemplateInfo("foo", identity: "foo.200", groupIdentity: "foo.group", precedence: 200),
                    new MockTemplateInfo("bar", identity: "bar.200", groupIdentity: "bar.group", precedence: 200),
            };

            yield return new object[] { new MockNewCommandInput("foo"), templates, null, (int)TemplateResolutionResult.Status.SingleMatch, "foo.200" };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.bar", groupIdentity: "foo.group").WithParameters("bar"),
                    new MockTemplateInfo("foo", identity: "foo.baz", groupIdentity: "foo.group").WithParameters("baz"),
            };
            yield return new object[] { new MockNewCommandInput("foo").WithTemplateOption("baz", "whatever"), templates, null, (int)TemplateResolutionResult.Status.SingleMatch, "foo.baz" };

            yield return new object[] { new MockNewCommandInput("foo").WithTemplateOption("bat", "whatever"), templates, null, (int)TemplateResolutionResult.Status.InvalidParameter, null };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group").WithTag("framework", "netcoreapp2.1", "netcoreapp3.1"),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group").WithTag("framework", "net5.0"),
            };

            yield return new object[] { new MockNewCommandInput("foo").WithTemplateOption("framework", "net5.0"), templates, null, (int)TemplateResolutionResult.Status.SingleMatch, "foo.2" };
            yield return new object[] { new MockNewCommandInput("foo").WithTemplateOption("framework", "netcoreapp2.0"), templates, null, (int)TemplateResolutionResult.Status.InvalidParameter, null };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group", precedence: 100).WithTag("MyChoice", "value_1"),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group", precedence: 200).WithTag("MyChoice", "value_2"),
            };

            yield return new object[] { new MockNewCommandInput("foo").WithTemplateOption("MyChoice", "value_"), templates, null, (int)TemplateResolutionResult.Status.AmbiguousParameterValueChoice, null };
            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group", precedence: 100).WithTag("MyChoice", "value_1"),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group", precedence: 200).WithTag("MyChoice", "value_2", "value_3"),
            };
            yield return new object[] { new MockNewCommandInput("foo").WithTemplateOption("MyChoice", "value_"), templates, null, (int)TemplateResolutionResult.Status.AmbiguousParameterValueChoice, null };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group", precedence: 100).WithTag("MyChoice", "value_1", "value_2"),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group", precedence: 200).WithTag("MyChoice", "value_3", "value_4"),
            };
            yield return new object[] { new MockNewCommandInput("foo").WithTemplateOption("MyChoice", "value_"), templates, null, (int)TemplateResolutionResult.Status.AmbiguousParameterValueChoice, null };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group", precedence: 100)
                        .WithTag("MyChoice", "value_1", "other_value")
                        .WithTag("OtherChoice", "foo_"),

                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group", precedence: 200)
                        .WithTag("MyChoice", "value_")
                        .WithTag("OtherChoice", "foo_", "bar_1"),
            };
            yield return new object[] { new MockNewCommandInput("foo").WithTemplateOption("MyChoice", "value_").WithTemplateOption("OtherChoice", "foo_"), templates, null, (int)TemplateResolutionResult.Status.SingleMatch, "foo.2" };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.1.FSharp", groupIdentity: "foo.group", precedence: 100)
                        .WithTag("language", "F#")
            };
            yield return new object[] { new MockNewCommandInput("foo"), templates, null, (int)TemplateResolutionResult.Status.SingleMatch, "foo.1.FSharp" };
            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.1.FSharp", groupIdentity: "foo.group", precedence: 100)
                        .WithTag("language", "F#"),
                    new MockTemplateInfo("foo", identity: "foo.1.VB", groupIdentity: "foo.group", precedence: 200)
                        .WithTag("language", "VB")
            };
            yield return new object[] { new MockNewCommandInput("foo"), templates, null, (int)TemplateResolutionResult.Status.SingleMatch, "foo.1.VB" };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.1.FSharp", groupIdentity: "foo.group", precedence: 200)
                        .WithTag("language", "F#"),
                    new MockTemplateInfo("foo", identity: "foo.1.VB", groupIdentity: "foo.group", precedence: 200)
                        .WithTag("language", "VB")
            };
            yield return new object[] { new MockNewCommandInput("foo"), templates, null, (int)TemplateResolutionResult.Status.AmbiguousLanguageChoice, null };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.1.FSharp", groupIdentity: "foo.group", precedence: 200)
                        .WithTag("language", "F#"),
                    new MockTemplateInfo("foo", identity: "foo.1.VB", groupIdentity: "foo.group", precedence: 200)
                        .WithTag("language", "")
            };
            yield return new object[] { new MockNewCommandInput("foo"), templates, null, (int)TemplateResolutionResult.Status.AmbiguousLanguageChoice, null };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.1.FSharp", groupIdentity: "foo.group", precedence: 200)
                        .WithTag("language", "F#"),
                    new MockTemplateInfo("foo", identity: "foo.1.VB", groupIdentity: "foo.group", precedence: 200)
            };
            yield return new object[] { new MockNewCommandInput("foo"), templates, null, (int)TemplateResolutionResult.Status.AmbiguousLanguageChoice, null };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.1.FSharp", groupIdentity: "foo.group", precedence: 200),
                    new MockTemplateInfo("foo", identity: "foo.1.VB", groupIdentity: "foo.group", precedence: 200)
            };
            yield return new object[] { new MockNewCommandInput("foo"), templates, null, (int)TemplateResolutionResult.Status.AmbiguousTemplateChoice, null };

            templates = new MockTemplateInfo[]
            {
                    new MockTemplateInfo("foo", identity: "foo.1.FSharp", groupIdentity: "foo.group", precedence: 200)
                        .WithTag("language", "F#"),
                    new MockTemplateInfo("foo", identity: "foo.1.VB", groupIdentity: "foo.group", precedence: 200)
                        .WithTag("language", "F#")
            };
            yield return new object[] { new MockNewCommandInput("foo"), templates, null, (int)TemplateResolutionResult.Status.AmbiguousTemplateChoice, null };
        }

        [Theory(DisplayName = nameof(TemplateResolution_TemplateToInvoke_Test))]
        [MemberData(nameof(Get_TemplateResolution_TemplateToInvoke_TestData))]
        internal void TemplateResolution_TemplateToInvoke_Test(MockNewCommandInput command, MockTemplateInfo[] templateSet, string defaultLanguage, int expectedStatus, string expectedIdentity)
        {
            TemplateResolutionResult matchResult = TemplateResolver.GetTemplateResolutionResult(templateSet, new MockHostSpecificDataLoader(), command, defaultLanguage);

            Assert.Equal(expectedStatus, (int)matchResult.ResolutionStatus);

            //compatibility with old methods
            Assert.Equal(expectedStatus == (int)TemplateResolutionResult.Status.SingleMatch,
                matchResult.TryGetSingularInvokableMatch(out ITemplateMatchInfo templateToInvoke, out TemplateResolutionResult.Status tryStatus));
            Assert.Equal(expectedStatus, (int)tryStatus);

            if (expectedStatus == (int)TemplateResolutionResult.Status.SingleMatch)
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
    }
}
