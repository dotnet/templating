using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Mocks;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests
{
    public class TelemetryHelperTests
    {
        [Fact(DisplayName = nameof(NonChoiceParameterHasNullCanonicalValueTest))]
        public void NonChoiceParameterHasNullCanonicalValueTest()
        {
            ITemplateParameter param = new MockParameter()
            {
                Name = "TestName",
                Choices = null
            };
            IReadOnlyList<ITemplateParameter> parametersForTemplate = new List<ITemplateParameter>() { param };

            ITemplateInfo template = new MockTemplate()
            {
                Parameters = parametersForTemplate
            };

            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(template, "TestName", "whatever");
            Assert.Null(canonical);
        }

        [Fact(DisplayName = nameof(UnknownParameterNameHasNullCanonicalValueTest))]
        public void UnknownParameterNameHasNullCanonicalValueTest()
        {
            ITemplateParameter param = new MockParameter()
            {
                Name = "TestName",
                Choices = null
            };
            IReadOnlyList<ITemplateParameter> parametersForTemplate = new List<ITemplateParameter>() { param };

            ITemplateInfo template = new MockTemplate()
            {
                Parameters = parametersForTemplate
            };

            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(template, "OtherName", "whatever");
            Assert.Null(canonical);
        }

        [Fact(DisplayName = nameof(InvalidChoiceValueForParameterHasNullCanonicalValueTest))]
        public void InvalidChoiceValueForParameterHasNullCanonicalValueTest()
        {
            ITemplateParameter param = new MockParameter()
            {
                Name = "TestName",
                Choices = new Dictionary<string, string>()
                {
                    { "foo", "Foo value" },
                    { "bar", "Bar value" }
                }
            };
            IReadOnlyList<ITemplateParameter> parametersForTemplate = new List<ITemplateParameter>() { param };

            ITemplateInfo template = new MockTemplate()
            {
                Parameters = parametersForTemplate
            };

            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(template, "TestName", "whatever");
            Assert.Null(canonical);
        }

        [Fact(DisplayName = nameof(ValidChoiceForParameterIsItsOwnCanonicalValueTest))]
        public void ValidChoiceForParameterIsItsOwnCanonicalValueTest()
        {
            ITemplateParameter param = new MockParameter()
            {
                Name = "TestName",
                Choices = new Dictionary<string, string>()
                {
                    { "foo", "Foo value" },
                    { "bar", "Bar value" }
                }
            };
            IReadOnlyList<ITemplateParameter> parametersForTemplate = new List<ITemplateParameter>() { param };

            ITemplateInfo template = new MockTemplate()
            {
                Parameters = parametersForTemplate
            };

            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(template, "TestName", "foo");
            Assert.Equal("foo", canonical);
        }

        [Fact(DisplayName = nameof(UniqueStartsWithValueResolvesCanonicalValueTest))]
        public void UniqueStartsWithValueResolvesCanonicalValueTest()
        {
            ITemplateParameter param = new MockParameter()
            {
                Name = "TestName",
                Choices = new Dictionary<string, string>()
                {
                    { "foo", "Foo value" },
                    { "bar", "Bar value" }
                }
            };
            IReadOnlyList<ITemplateParameter> parametersForTemplate = new List<ITemplateParameter>() { param };

            ITemplateInfo template = new MockTemplate()
            {
                Parameters = parametersForTemplate
            };

            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(template, "TestName", "f");
            Assert.Equal("foo", canonical);
        }

        [Fact(DisplayName = nameof(AmbiguousStartsWithValueHasNullCanonicalValueTest))]
        public void AmbiguousStartsWithValueHasNullCanonicalValueTest()
        {
            ITemplateParameter param = new MockParameter()
            {
                Name = "TestName",
                Choices = new Dictionary<string, string>()
                {
                    { "foo", "Foo value" },
                    { "bar", "Bar value" },
                    { "foot", "Foot value" }
                }
            };
            IReadOnlyList<ITemplateParameter> parametersForTemplate = new List<ITemplateParameter>() { param };

            ITemplateInfo template = new MockTemplate()
            {
                Parameters = parametersForTemplate
            };

            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(template, "TestName", "f");
            Assert.Null(canonical);
        }

        [Fact(DisplayName = nameof(ChoiceValueCaseDifferenceIsAMatchTest))]
        public void ChoiceValueCaseDifferenceIsAMatchTest()
        {
            ITemplateParameter param = new MockParameter()
            {
                Name = "TestName",
                Choices = new Dictionary<string, string>()
                {
                    { "foo", "Foo value" },
                    { "bar", "Bar value" }
                }
            };
            IReadOnlyList<ITemplateParameter> parametersForTemplate = new List<ITemplateParameter>() { param };

            ITemplateInfo template = new MockTemplate()
            {
                Parameters = parametersForTemplate
            };

            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(template, "TestName", "FOO");
            Assert.Equal("foo", canonical);
        }

        [Fact(DisplayName = nameof(ChoiceValueCaseDifferencesContributeToAmbiguousMatchTest))]
        public void ChoiceValueCaseDifferencesContributeToAmbiguousMatchTest()
        {
            ITemplateParameter param = new MockParameter()
            {
                Name = "TestName",
                Choices = new Dictionary<string, string>()
                {
                    { "foot", "Foo value" },
                    { "bar", "Bar value" },
                    { "Football", "Foo value" },
                    { "FOOTPOUND", "Foo value" }
                }
            };
            IReadOnlyList<ITemplateParameter> parametersForTemplate = new List<ITemplateParameter>() { param };

            ITemplateInfo template = new MockTemplate()
            {
                Parameters = parametersForTemplate
            };

            string canonical = TelemetryHelper.GetCanonicalValueForChoiceParamOrDefault(template, "TestName", "foo");
            Assert.Null(canonical);
        }
    }
}
