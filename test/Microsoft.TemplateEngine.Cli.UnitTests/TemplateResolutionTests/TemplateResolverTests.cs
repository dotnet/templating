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
                true,
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
                true,
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
                true,
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
                true,
                new string [] { "foo.test.new" }
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
                true,
                new string [] { "foo.test.old" }
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
                true,
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
                true,
                new string [] { "foo.test.1x", "foo.test.2x" }
            };
        }

        [Theory(DisplayName = nameof(TemplateResolution_UnambiguousGroup_Test))]
        [MemberData(nameof(Get_TemplateResolution_UnambiguousGroup_TestData))]
        internal void TemplateResolution_UnambiguousGroup_Test(MockNewCommandInput command, MockTemplateInfo[] templateSet, string defaultLanguage, bool expectedStatus, string[] expectedIdentities)
        {
            var matchResult = TemplateResolver.GetTemplateResolutionResult(templateSet, new MockHostSpecificDataLoader(), command, defaultLanguage);

            //compatibility with old methods
            Assert.Equal(expectedStatus, matchResult.TryGetUnambiguousTemplateGroupToUse(out IReadOnlyList<ITemplateMatchInfo> unambiguousGroup));

            if (expectedStatus)
            {
                var identities = unambiguousGroup.Select(t => t.Info.Identity);
                Assert.Equal(expectedIdentities.Length, identities.Count());
                foreach (string identity in expectedIdentities)
                {
                    Assert.Single(identities.Where(i => i == identity));
                }
            }
            else
            {
                Assert.Null(unambiguousGroup);
            }
        }
    }
}
