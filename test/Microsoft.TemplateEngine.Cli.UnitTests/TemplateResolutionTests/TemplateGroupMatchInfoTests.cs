// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using FakeItEasy;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.HelpAndUsage;
using Microsoft.TemplateEngine.Cli.TemplateResolution;
using Microsoft.TemplateEngine.Cli.UnitTests.CliMocks;
using Microsoft.TemplateEngine.Mocks;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests.TemplateResolutionTests
{
    public class TemplateGroupMatchInfoTests
    {
        public static IEnumerable<object[]> GetInvalidParametersTestData()
        {
            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("--framework", "netcoreapp3.0"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group").WithChoiceParameter("framework", "netcoreapp2.1", "netcoreapp3.1"),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group").WithChoiceParameter("framework", "net5.0"),
                },
                new MockInvalidParameterInfo[]
                {
                    new MockInvalidParameterInfo(InvalidParameterInfo.Kind.InvalidParameterValue, "--framework", "netcoreapp3.0")
                }
            };

            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("--fake", "netcoreapp3.0"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group").WithChoiceParameter("framework", "netcoreapp2.1", "netcoreapp3.1"),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group").WithChoiceParameter("framework", "net5.0"),
                },
                new MockInvalidParameterInfo[]
                {
                    new MockInvalidParameterInfo(InvalidParameterInfo.Kind.InvalidParameterName, "--fake", null)
                }
            };

            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("--framework", "net"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group").WithChoiceParameter("framework", "netcoreapp2.1", "netcoreapp3.1"),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group").WithChoiceParameter("framework", "net5.0"),
                },
                new MockInvalidParameterInfo[]
                {
                    new MockInvalidParameterInfo(InvalidParameterInfo.Kind.InvalidParameterValue, "--framework", "net")
                }
            };

            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("--framework", "net"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group").WithChoiceParameter("framework", "netcoreapp2.1"),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group").WithChoiceParameter("framework", "net5.0"),
                    new MockTemplateInfo("foo", identity: "foo.3", groupIdentity: "foo.group").WithChoiceParameter("framework", "netcoreapp3.1"),
                },
                new MockInvalidParameterInfo[]
                {
                    new MockInvalidParameterInfo(InvalidParameterInfo.Kind.InvalidParameterValue, "--framework", "net")
                }
            };

            yield return new object[]
            {
                new MockNewCommandInput("foo").WithTemplateOption("--framework", "net").WithTemplateOption("--fake", "fake").WithTemplateOption("--OtherChoice", "fake"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group").WithChoiceParameter("framework", "netcoreapp2.1", "netcoreapp3.1").WithChoiceParameter("OtherChoice", "val1", "val2"),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group").WithChoiceParameter("framework", "net5.0").WithChoiceParameter("OtherChoice", "val1", "val2"),
                },
                new MockInvalidParameterInfo[]
                {
                    new MockInvalidParameterInfo(InvalidParameterInfo.Kind.InvalidParameterValue, "--framework", "net"),
                    new MockInvalidParameterInfo(InvalidParameterInfo.Kind.InvalidParameterName, "--fake", null),
                    new MockInvalidParameterInfo(InvalidParameterInfo.Kind.InvalidParameterValue, "--OtherChoice", "fake")
                }
            };
        }

        public static IEnumerable<object?[]> GetHighestPrecedenceInvokableTemplatesTestData()
        {
            yield return new object?[]
            {
                new MockNewCommandInput("foo"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group", precedence: 100),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group", precedence: 200),
                    new MockTemplateInfo("foo", identity: "foo.3", groupIdentity: "foo.group", precedence: 300),
                },
                null,
                new string[] { "foo.3" }
            };

            yield return new object?[]
            {
                new MockNewCommandInput("foo"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group", precedence: 300),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group", precedence: 300),
                    new MockTemplateInfo("foo", identity: "foo.3", groupIdentity: "foo.group", precedence: 300),
                },
                null,
                new string[] { "foo.1", "foo.2", "foo.3" }
            };

            yield return new object[]
            {
                new MockNewCommandInput("foo", language: "VB"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group", precedence: 300).WithTag("language", "VB"),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group", precedence: 300).WithTag("language", "F#"),
                    new MockTemplateInfo("foo", identity: "foo.3", groupIdentity: "foo.group", precedence: 300).WithTag("language", "Lisp"),
                },
                "Lisp",
                new string[] { "foo.1" }
            };

            yield return new object[]
            {
                new MockNewCommandInput("foo"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group", precedence: 300).WithTag("language", "VB"),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group", precedence: 300).WithTag("language", "F#"),
                    new MockTemplateInfo("foo", identity: "foo.3", groupIdentity: "foo.group", precedence: 300).WithTag("language", "Lisp"),
                },
                "Lisp",
                new string[] { "foo.3" }
            };

            yield return new object?[]
            {
                new MockNewCommandInput("foo"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group", precedence: 300).WithTag("language", "VB"),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group", precedence: 300).WithTag("language", "F#"),
                    new MockTemplateInfo("foo", identity: "foo.3", groupIdentity: "foo.group", precedence: 300).WithTag("language", "Lisp"),
                },
                null,
                new string[] { "foo.1", "foo.2", "foo.3" }
            };

            yield return new object?[]
            {
                new MockNewCommandInput("foo"),
                new MockTemplateInfo[]
                {
                    new MockTemplateInfo("foo", identity: "foo.1", groupIdentity: "foo.group", precedence: 100).WithTag("language", "VB"),
                    new MockTemplateInfo("foo", identity: "foo.2", groupIdentity: "foo.group", precedence: 200).WithTag("language", "F#"),
                    new MockTemplateInfo("foo", identity: "foo.3", groupIdentity: "foo.group", precedence: 300).WithTag("language", "Lisp"),
                },
                null,
                new string[] { "foo.3" }
            };
        }

        [Theory(DisplayName = nameof(GetInvalidParametersTest))]
        [MemberData(nameof(GetInvalidParametersTestData))]
        internal async Task GetInvalidParametersTest(MockNewCommandInput command, MockTemplateInfo[] templates, MockInvalidParameterInfo[] expectedInvalidParams)
        {
            InstantiateTemplateResolver resolver = new InstantiateTemplateResolver(templates, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(command, defaultLanguage: null, default).ConfigureAwait(false);

            Assert.NotNull(matchResult.UnambiguousTemplateGroupMatchInfo);
            TemplateGroupMatchInfo templateGroup = matchResult.UnambiguousTemplateGroupMatchInfo!;
            IEnumerable<InvalidParameterInfo> invalidParameters = templateGroup.GetInvalidParameterList();

            Assert.Equal(expectedInvalidParams.Length, invalidParameters.Count());
            foreach (MockInvalidParameterInfo exp in expectedInvalidParams)
            {
                Assert.Single(invalidParameters.Where(param => param.ErrorKind == exp.Kind && param.InputFormat == exp.InputFormat && param.SpecifiedValue == exp.SpecifiedValue));
            }
        }

        [Theory(DisplayName = nameof(GetHighestPrecedenceInvokableTemplatesTest))]
        [MemberData(nameof(GetHighestPrecedenceInvokableTemplatesTestData))]
        internal async Task GetHighestPrecedenceInvokableTemplatesTest(MockNewCommandInput command, MockTemplateInfo[] templates, string defaultLanguage, string[] expectedTemplates)
        {
            InstantiateTemplateResolver resolver = new InstantiateTemplateResolver(templates, new MockHostSpecificDataLoader());
            TemplateResolutionResult matchResult = await resolver.ResolveTemplatesAsync(command, defaultLanguage: defaultLanguage, default).ConfigureAwait(false);

            Assert.NotNull(matchResult.UnambiguousTemplateGroupMatchInfo);
            TemplateGroupMatchInfo templateGroup = matchResult.UnambiguousTemplateGroupMatchInfo!;
            bool useDefaultLanguage = string.IsNullOrWhiteSpace(command.Language) && !string.IsNullOrWhiteSpace(defaultLanguage);
            IEnumerable<ITemplateInfo> selectedTemplates = templateGroup.GetHighestPrecedenceTemplates();

            var identitiesToCheck = selectedTemplates.Select(t => t.Identity);

            Assert.Equal(expectedTemplates.Length, selectedTemplates.Count());
            foreach (string exp in expectedTemplates)
            {
                Assert.Single(selectedTemplates.Where(t => t.Identity == exp));
            }
        }
    }
}
