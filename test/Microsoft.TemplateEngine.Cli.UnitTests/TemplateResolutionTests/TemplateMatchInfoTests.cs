using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.TemplateResolution;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Mocks;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests
{
    public class TemplateMatchInfoTests
    {
        [Fact(DisplayName = nameof(EmptyMatchDisposition_ReportsCorrectly))]
        public void EmptyMatchDisposition_ReportsCorrectly()
        {
            ITemplateInfo templateInfo = new MockTemplateInfo();
            ITemplateMatchInfo TemplateMatchInfo = new TemplateMatchInfo(templateInfo);
            Assert.False(TemplateMatchInfo.IsMatch);
            Assert.False(TemplateMatchInfo.IsPartialMatch);
            Assert.False(TemplateMatchInfo.IsInvokableMatch());
            Assert.False(TemplateMatchInfo.HasAmbiguousParameterValueMatch());
            Assert.Equal(0, TemplateMatchInfo.GetInvalidParameterNames().Count);
            Assert.Equal(0, TemplateMatchInfo.GetValidTemplateParameters().Count);
        }

        [Fact(DisplayName = nameof(NameExactMatch_ReportsCorrectly))]
        public void NameExactMatch_ReportsCorrectly()
        {
            ITemplateInfo templateInfo = new MockTemplateInfo();
            ITemplateMatchInfo TemplateMatchInfo = new TemplateMatchInfo(templateInfo);
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Name, "test", MatchKind.Exact));
            Assert.True(TemplateMatchInfo.IsMatch);
            Assert.True(TemplateMatchInfo.IsPartialMatch);
            Assert.True(TemplateMatchInfo.IsInvokableMatch());
            Assert.False(TemplateMatchInfo.HasAmbiguousParameterValueMatch());
        }

        [Fact(DisplayName = nameof(NamePartialMatch_ReportsCorrectly))]
        public void NamePartialMatch_ReportsCorrectly()
        {
            ITemplateInfo templateInfo = new MockTemplateInfo();
            ITemplateMatchInfo TemplateMatchInfo = new TemplateMatchInfo(templateInfo);
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Name, "test", MatchKind.Partial));
            Assert.True(TemplateMatchInfo.IsMatch);
            Assert.True(TemplateMatchInfo.IsPartialMatch);
            Assert.True(TemplateMatchInfo.IsInvokableMatch());
            Assert.False(TemplateMatchInfo.HasAmbiguousParameterValueMatch());
        }

        [Fact(DisplayName = nameof(NameMismatch_ReportsCorrectly))]
        public void NameMismatch_ReportsCorrectly()
        {
            ITemplateInfo templateInfo = new MockTemplateInfo();
            ITemplateMatchInfo TemplateMatchInfo = new TemplateMatchInfo(templateInfo);
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Name, "test", MatchKind.Mismatch));
            Assert.False(TemplateMatchInfo.IsMatch);
            Assert.False(TemplateMatchInfo.IsPartialMatch);
            Assert.False(TemplateMatchInfo.IsInvokableMatch());
            Assert.False(TemplateMatchInfo.HasAmbiguousParameterValueMatch());
        }

        [Fact(DisplayName = nameof(TypeMatch_ReportsCorrectly))]
        public void TypeMatch_ReportsCorrectly()
        {
            ITemplateInfo templateInfo = new MockTemplateInfo();
            ITemplateMatchInfo TemplateMatchInfo = new TemplateMatchInfo(templateInfo);
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Type, "test", MatchKind.Exact));
            Assert.True(TemplateMatchInfo.IsMatch);
            Assert.True(TemplateMatchInfo.IsPartialMatch);
            Assert.True(TemplateMatchInfo.IsInvokableMatch());
            Assert.False(TemplateMatchInfo.HasAmbiguousParameterValueMatch());
        }

        [Fact(DisplayName = nameof(TypeMismatch_ReportsCorrectly))]
        public void TypeMismatch_ReportsCorrectly()
        {
            ITemplateInfo templateInfo = new MockTemplateInfo();
            ITemplateMatchInfo TemplateMatchInfo = new TemplateMatchInfo(templateInfo);
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Type, "test", MatchKind.Mismatch));
            Assert.False(TemplateMatchInfo.IsMatch);
            Assert.False(TemplateMatchInfo.IsPartialMatch);
            Assert.False(TemplateMatchInfo.IsInvokableMatch());
            Assert.False(TemplateMatchInfo.HasAmbiguousParameterValueMatch());
        }

        [Fact(DisplayName = nameof(TypeMatch_NameMatch_ReportsCorrectly))]
        public void TypeMatch_NameMatch_ReportsCorrectly()
        {
            ITemplateInfo templateInfo = new MockTemplateInfo();
            ITemplateMatchInfo TemplateMatchInfo = new TemplateMatchInfo(templateInfo);
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Name, "test", MatchKind.Exact));
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Type, "test", MatchKind.Exact));
            Assert.True(TemplateMatchInfo.IsMatch);
            Assert.True(TemplateMatchInfo.IsPartialMatch);
            Assert.True(TemplateMatchInfo.IsInvokableMatch());
            Assert.False(TemplateMatchInfo.HasAmbiguousParameterValueMatch());
        }

        [Fact(DisplayName = nameof(TypeMatch_NameMismatch_ReportsCorrectly))]
        public void TypeMatch_NameMismatch_ReportsCorrectly()
        {
            ITemplateInfo templateInfo = new MockTemplateInfo();
            ITemplateMatchInfo TemplateMatchInfo = new TemplateMatchInfo(templateInfo);
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Name, "test", MatchKind.Mismatch));
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Type, "test", MatchKind.Exact));
            Assert.False(TemplateMatchInfo.IsMatch);
            Assert.True(TemplateMatchInfo.IsPartialMatch);
            Assert.False(TemplateMatchInfo.IsInvokableMatch());
            Assert.False(TemplateMatchInfo.HasAmbiguousParameterValueMatch());
        }

        [Fact(DisplayName = nameof(TypeMatch_NamePartialMatch_ReportsCorrectly))]
        public void TypeMatch_NamePartialMatch_ReportsCorrectly()
        {
            ITemplateInfo templateInfo = new MockTemplateInfo();
            ITemplateMatchInfo TemplateMatchInfo = new TemplateMatchInfo(templateInfo);
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Name, "test", MatchKind.Partial));
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Type, "test", MatchKind.Exact));
            Assert.True(TemplateMatchInfo.IsMatch);
            Assert.True(TemplateMatchInfo.IsPartialMatch);
            Assert.True(TemplateMatchInfo.IsInvokableMatch());
            Assert.False(TemplateMatchInfo.HasAmbiguousParameterValueMatch());
        }

        [Fact(DisplayName = nameof(TypeMismatch_NameMatch_ReportsCorrectly))]
        public void TypeMismatch_NameMatch_ReportsCorrectly()
        {
            ITemplateInfo templateInfo = new MockTemplateInfo();
            ITemplateMatchInfo TemplateMatchInfo = new TemplateMatchInfo(templateInfo);
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Name, "test", MatchKind.Exact));
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Type, "test", MatchKind.Mismatch));
            Assert.False(TemplateMatchInfo.IsMatch);
            Assert.True(TemplateMatchInfo.IsPartialMatch);
            Assert.False(TemplateMatchInfo.IsInvokableMatch());
            Assert.False(TemplateMatchInfo.HasAmbiguousParameterValueMatch());
        }

        [Fact(DisplayName = nameof(TypeMismatch_NamePartialMatch_ReportsCorrectly))]
        public void TypeMismatch_NamePartialMatch_ReportsCorrectly()
        {
            ITemplateInfo templateInfo = new MockTemplateInfo();
            ITemplateMatchInfo TemplateMatchInfo = new TemplateMatchInfo(templateInfo);
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Name, "test", MatchKind.Partial));
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Type, "test", MatchKind.Mismatch));
            TemplateMatchInfo.AddDisposition(new MatchInfo(MatchInfo.DefaultParameter.Type, "test", MatchKind.Mismatch));
            Assert.False(TemplateMatchInfo.IsMatch);
            Assert.True(TemplateMatchInfo.IsPartialMatch);
            Assert.False(TemplateMatchInfo.IsInvokableMatch());
            Assert.False(TemplateMatchInfo.HasAmbiguousParameterValueMatch());
        }
    }
}
