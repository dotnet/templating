// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using FakeItEasy;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests
{
    public class TelemetryHelperTests
    {
        [Fact(DisplayName = nameof(NonChoiceParameterHasNullCanonicalValueTest))]
        public void NonChoiceParameterHasNullCanonicalValueTest()
        {
            ITemplateInfo templateInfo = A.Fake<ITemplateInfo>();
            A.CallTo(() => templateInfo.CacheParameters).Returns(new Dictionary<string, ICacheParameter>()
            {
                { "TestName", new CacheParameter() }
            });

            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(templateInfo, "TestName", "whatever");
            Assert.Null(canonical);
        }

        [Fact(DisplayName = nameof(UnknownParameterNameHasNullCanonicalValueTest))]
        public void UnknownParameterNameHasNullCanonicalValueTest()
        {
            ITemplateInfo templateInfo = A.Fake<ITemplateInfo>();
            A.CallTo(() => templateInfo.CacheParameters).Returns(new Dictionary<string, ICacheParameter>()
            {
                { "TestName", new CacheParameter() }
            });
            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(templateInfo, "OtherName", "whatever");
            Assert.Null(canonical);
        }

        [Fact(DisplayName = nameof(InvalidChoiceValueForParameterHasNullCanonicalValueTest))]
        public void InvalidChoiceValueForParameterHasNullCanonicalValueTest()
        {
            ITemplateInfo templateInfo = A.Fake<ITemplateInfo>();
            A.CallTo(() => templateInfo.Tags).Returns(new Dictionary<string, ICacheTag>()
            {
                {
                    "TestName",
                    new CacheTag(
                        null,
                        null,
                        new Dictionary<string, ParameterChoice>()
                        {
                            { "foo", new ParameterChoice("Foo", "Foo value") },
                            { "bar", new ParameterChoice("Bar", "Bar value") }
                        },
                        null
                    )
                }
            });

            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(templateInfo, "TestName", "whatever");
            Assert.Null(canonical);
        }

        [Fact(DisplayName = nameof(ValidChoiceForParameterIsItsOwnCanonicalValueTest))]
        public void ValidChoiceForParameterIsItsOwnCanonicalValueTest()
        {
            ITemplateInfo templateInfo = A.Fake<ITemplateInfo>();
            A.CallTo(() => templateInfo.Tags).Returns(new Dictionary<string, ICacheTag>()
            {
                {
                    "TestName",
                    new CacheTag(
                        null,
                        null,
                        new Dictionary<string, ParameterChoice>()
                        {
                            { "foo", new ParameterChoice("Foo", "Foo value") },
                            { "bar", new ParameterChoice("Bar", "Bar value") }
                        },
                        null
                    )
                }
            });

            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(templateInfo, "TestName", "foo");
            Assert.Equal("foo", canonical);
        }

        [Fact(DisplayName = nameof(UniqueStartsWithValueResolvesCanonicalValueTest))]
        public void UniqueStartsWithValueResolvesCanonicalValueTest()
        {
            ITemplateInfo templateInfo = A.Fake<ITemplateInfo>();
            A.CallTo(() => templateInfo.Tags).Returns(new Dictionary<string, ICacheTag>()
            {
                {
                    "TestName",
                    new CacheTag(
                        null,
                        null,
                        new Dictionary<string, ParameterChoice>()
                        {
                            { "foo", new ParameterChoice("Foo", "Foo value") },
                            { "bar", new ParameterChoice("Bar", "Bar value") }
                        },
                        null
                    )
                }
            });

            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(templateInfo, "TestName", "f");
            Assert.Equal("foo", canonical);
        }

        [Fact(DisplayName = nameof(AmbiguousStartsWithValueHasNullCanonicalValueTest))]
        public void AmbiguousStartsWithValueHasNullCanonicalValueTest()
        {
            ITemplateInfo templateInfo = A.Fake<ITemplateInfo>();
            A.CallTo(() => templateInfo.Tags).Returns(new Dictionary<string, ICacheTag>()
            {
                {
                    "TestName",
                    new CacheTag(
                        null,
                        null,
                        new Dictionary<string, ParameterChoice>()
                        {
                            { "foo", new ParameterChoice("Foo", "Foo value") },
                            { "bar", new ParameterChoice("Bar", "Bar value") },
                            { "foot", new ParameterChoice("Foot", "Foot value") }
                        },
                        null
                    )
                }
            });

            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(templateInfo, "TestName", "f");
            Assert.Null(canonical);
        }

        [Fact(DisplayName = nameof(ChoiceValueCaseDifferenceIsAMatchTest))]
        public void ChoiceValueCaseDifferenceIsAMatchTest()
        {
            ITemplateInfo templateInfo = A.Fake<ITemplateInfo>();
            A.CallTo(() => templateInfo.Tags).Returns(new Dictionary<string, ICacheTag>()
            {
                {
                    "TestName",
                    new CacheTag(
                        null,
                        null,
                        new Dictionary<string, ParameterChoice>()
                        {
                            { "foo", new ParameterChoice("Foo", "Foo value") },
                            { "bar", new ParameterChoice("Bar", "Bar value") }
                        },
                        null
                    )
                }
            });
            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(templateInfo, "TestName", "FOO");
            Assert.Equal("FOO", canonical);
        }

        [Fact(DisplayName = nameof(ChoiceValueCaseDifferencesContributeToAmbiguousMatchTest))]
        public void ChoiceValueCaseDifferencesContributeToAmbiguousMatchTest()
        {
            ITemplateInfo templateInfo = A.Fake<ITemplateInfo>();
            A.CallTo(() => templateInfo.Tags).Returns(new Dictionary<string, ICacheTag>()
            {
                {
                    "TestName",
                    new CacheTag(
                        null,
                        null,
                        new Dictionary<string, ParameterChoice>()
                        {
                            { "foot", new ParameterChoice("Foo", "Foo value") },
                            { "bar", new ParameterChoice("Bar", "Bar value") },
                            { "Football", new ParameterChoice("Football", "Foo value") },
                            { "FOOTPOUND", new ParameterChoice("Footpound", "Foo value") }
                        },
                        null
                    )
                }
            });

            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(templateInfo, "TestName", "foo");
            Assert.Null(canonical);
        }
    }
}
