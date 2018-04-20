using System.IO;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Operations;
using Microsoft.TemplateEngine.Core.Util;
using Xunit;

namespace Microsoft.TemplateEngine.Core.UnitTests
{
    public class BlockTests : TestBase
    {
        private static IOperationProvider[] TestBlockOperations
        {
            get
            {
                IOperationProvider[] operations =
                {
                    new TestBlockOperationProvider("abc123", "#block 1".TokenConfig(), "#endblock 1".TokenConfig(), true),
                };

                return operations;
            }
        }

        private IProcessor SetupTestProcessor(IOperationProvider[] operations, VariableCollection vc)
        {
            EngineConfig cfg = new EngineConfig(EnvironmentSettings, vc);
            return Processor.Create(cfg, operations);
        }


        [Fact(DisplayName = nameof(TestBlockIsolatesCorrectData))]
        public void TestBlockIsolatesCorrectData()
        {
            VariableCollection vc = new VariableCollection();
            IProcessor processor = SetupTestProcessor(TestBlockOperations, vc);

            const string originalValue = @"before
#block 1
contents

 #endblock 1
after";

            const string expectedValue = @"before
contents

after";

            RunAndVerify(originalValue, expectedValue, processor, 9999);
        }


        public class TestBlockOperationProvider : BlockOperationProviderBase
        {
            public TestBlockOperationProvider(string id, ITokenConfig startToken, ITokenConfig endToken, bool isInitialStateOn)
                : base(id, startToken, endToken, isInitialStateOn)
            {
            }

            public override string OperationName { get; } = "testblock";

            protected override ImplBase Create(string id, BlockOperationProviderBase owner, ITokenTrie matcher, bool isInitialStateOn)
            {
                return new Impl(id, owner, matcher, isInitialStateOn);
            }

            protected class Impl : ImplBase
            {
                public Impl(string id, BlockOperationProviderBase owner, ITokenTrie matcher, bool isInitialStateOn)
                    : base(id, owner, matcher, isInitialStateOn)
                {
                }

                protected override int OnBlockIsolated(IProcessorState outerProcessor, Stream blockData, Stream target)
                {
                    blockData.CopyTo(target);
                    return (int)blockData.Length;
                }
            }
        }
    }
}
