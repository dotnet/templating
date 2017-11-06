using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests
{
    public class ConditionalConfigurationTests : EndToEndTestBase
    {
        [Theory(DisplayName = nameof(VerifyConditionalConfiguration))]
        [InlineData("TestAssets.TemplateWithCustomGlobReferencingBuiltin --Extra true", "CustomGlobReferencingBuiltinConditionalTest.json")]
        [InlineData("TestAssets.TemplateWithBlockCommentConditionalConfiguration --Extra true", "CustomBlockCommentConditionalTests.json")]
        [InlineData("TestAssets.TemplateWithLineCommentConditionalConfiguration --Extra true", "CustomLineCommentConditionalTests.json")]
        [InlineData("TestAssets.TemplateWithMultipleBuiltInConditionalsOnOneGlob --Extra true", "MultipleBuiltInConditionalsTests.json")]
        public void VerifyConditionalConfiguration(string args, params string[] scripts)
        {
            Run(args, scripts);
        }
    }
}
