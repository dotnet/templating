using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Expressions.Cpp;
using Microsoft.TemplateEngine.Core.Operations;
using Microsoft.TemplateEngine.Core.Util;
using Xunit;

namespace Microsoft.TemplateEngine.Core.UnitTests
{
    public class InlineMarkupConditionalTests : TestBase
    {
        private static IProcessor SetupXmlPlusCppProcessor(IVariableCollection vc)
        {
            EngineConfig cfg = new EngineConfig(vc, "$({0})");
            return Processor.Create(cfg, new InlineMarkupConditional(
                new MarkupTokens("<", "</", ">", "/>", "Condition=\"", "\""),
                true,
                true,
                CppStyleEvaluatorDefinition.CppStyleEvaluator,
                null
            ));
        }

        [Fact]
        public void VerifyInlineMarkupTrue()
        {
            string originalValue = @"<root>
    <element Condition=""$(FIRST_IF)"" />
    <element Condition=""$(SECOND_IF)"">
        <child>
            <grandchild />
        </child>
    </element>
</root>";

            string expectedValue = @"<root>
    <element />
    <element>
        <child>
            <grandchild />
        </child>
    </element>
</root>";
            VariableCollection vc = new VariableCollection
            {
                ["FIRST_IF"] = true,
                ["SECOND_IF"] = true
            };
            IProcessor processor = SetupXmlPlusCppProcessor(vc);
            RunAndVerify(originalValue, expectedValue, processor, 9999);
        }

        [Fact]
        public void VerifyInlineMarkupSelfClosedFalse()
        {
            string originalValue = @"<root>
    <element Condition=""$(FIRST_IF)"" />
    <element Condition=""$(SECOND_IF)"">
        <child>
            <grandchild />
        </child>
    </element>
</root>";

            string expectedValue = @"<root>
    <element>
        <child>
            <grandchild />
        </child>
    </element>
</root>";
            VariableCollection vc = new VariableCollection
            {
                ["FIRST_IF"] = false,
                ["SECOND_IF"] = true
            };
            IProcessor processor = SetupXmlPlusCppProcessor(vc);
            RunAndVerify(originalValue, expectedValue, processor, 9999);
        }

        [Fact]
        public void VerifyInlineMarkupElementWithChildrenFalse()
        {
            string originalValue = @"<root>
    <element Condition=""$(FIRST_IF)"" />
    <element Condition=""$(SECOND_IF)"">
        <child>
            <grandchild />
        </child>
    </element>
</root>";

            string expectedValue = @"<root>
    <element />
</root>";
            VariableCollection vc = new VariableCollection
            {
                ["FIRST_IF"] = true,
                ["SECOND_IF"] = false
            };
            IProcessor processor = SetupXmlPlusCppProcessor(vc);
            RunAndVerify(originalValue, expectedValue, processor, 9999);
        }

        [Fact]
        public void VerifyInlineMarkupFalse()
        {
            string originalValue = @"<root>
    <element Condition=""$(FIRST_IF)"" />
    <element Condition=""$(SECOND_IF)"">
        <child>
            <grandchild />
        </child>
    </element>
</root>";

            string expectedValue = @"<root>
</root>";
            VariableCollection vc = new VariableCollection
            {
                ["FIRST_IF"] = false,
                ["SECOND_IF"] = false
            };
            IProcessor processor = SetupXmlPlusCppProcessor(vc);
            RunAndVerify(originalValue, expectedValue, processor, 9999);
        }
    }
}
