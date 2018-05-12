using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private static IOperationProvider[] ExtraReplaceOperations
        {
            get
            {
                IOperationProvider[] operations =
                {
                    new ExtraReplaceOperation("abc123", "#block 1".TokenConfig(), "#endblock 1".TokenConfig(), true),
                };

                return operations;
            }
        }

        private static IOperationProvider[] ForOperations(int iterations)
        {
            IOperationProvider[] operations =
            {
                new ForOperation("abc123", "#for".TokenConfig(), "#endfor".TokenConfig(), true, iterations),
                new ForOperation("abc1234", "#for2".TokenConfig(), "#endfor2".TokenConfig(), true, iterations),
            };

            return operations;
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


        [Fact(DisplayName = nameof(ExtraReplaceOperationEditsOnlyInBlock))]
        public void ExtraReplaceOperationEditsOnlyInBlock()
        {
            VariableCollection vc = new VariableCollection();
            IProcessor processor = SetupTestProcessor(ExtraReplaceOperations, vc);

            const string originalValue = @"contents
#block 1
contents

 #endblock 1
contents";

            const string expectedValue = @"contents
Hello there!

contents";

            RunAndVerify(originalValue, expectedValue, processor, 9999);
        }

        [Theory(DisplayName = nameof(ForOperationWorks))]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public void ForOperationWorks(int iterations)
        {
            VariableCollection vc = new VariableCollection();
            IProcessor processor = SetupTestProcessor(ForOperations(iterations), vc);

            const string originalValue = @"contents
#for
contents

 #endfor
contents";

            string expectedValue = @"contents
" + string.Join("\r\n", Enumerable.Repeat(@"Hello there!
", iterations)) + @"
contents";

            RunAndVerify(originalValue, expectedValue, processor, 9999);
        }


        [Theory(DisplayName = nameof(NestedForOperationWorks))]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(10)]
        public void NestedForOperationWorks(int iterations)
        {
            VariableCollection vc = new VariableCollection();
            IProcessor processor = SetupTestProcessor(ForOperations(iterations), vc);

            const string originalValue = @"contents
#for
    #for2
contents

    #endfor2
#endfor
contents";

            string expectedValue = @"contents
" + string.Join(Environment.NewLine, Enumerable.Repeat(@"Hello there!
", iterations * iterations)) + @"
contents";

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

        public class ExtraReplaceOperation : BlockOperationWithCustomProcessorProviderBase
        {
            public ExtraReplaceOperation(string id, ITokenConfig startToken, ITokenConfig endToken, bool isInitialStateOn)
                : base(id, startToken, endToken, isInitialStateOn, s => GenerateExtraReplace(id, s), x => null, (x, y) => { })
            {
            }

            private static IProcessor GenerateExtraReplace(string id, IProcessorState outerProcessor)
            {
                string variableName = $"ExtraReplace-{id}-RunCount";
                int runCount;
                if (!outerProcessor.Config.Variables.TryGetValue($"ExtraReplace-{id}-RunCount", out object runCountObject))
                {
                    outerProcessor.Config.Variables[variableName] = runCountObject = runCount = 0;
                }
                else
                {
                    runCount = (int)runCountObject;
                }

                if (runCount == 0 && outerProcessor is IOwnedProcessorState ownedState && ownedState.Processor != null)
                {
                    outerProcessor.Config.Variables[variableName] = runCount = 1;
                    return ownedState.Processor.CloneAndAppendOperations(new IOperationProvider[] { new Replacement("contents".TokenConfig(), "Hello there!", null, true) });
                }

                return null;
            }

            public override string OperationName => "extrareplace";
        }

        public class ForOperation : BlockOperationWithCustomProcessorProviderBase
        {
            public ForOperation(string id, ITokenConfig startToken, ITokenConfig endToken, bool isInitialStateOn, int iterations)
                : base(id, startToken, endToken, isInitialStateOn, s => GenerateExtraReplace(id, iterations, s), x => x.ToDictionary(y => y.Key, y => y.Value), (v, t) =>
                {
                    Dictionary<string, object> d = t as Dictionary<string, object>;
                    if (d == null)
                    {
                        return;
                    }

                    HashSet<string> keys = new HashSet<string>(v.Keys);

                    foreach (KeyValuePair<string, object> entry in d)
                    {
                        keys.Remove(entry.Key);
                        v[entry.Key] = entry.Value;
                    }

                    foreach (string key in keys)
                    {
                        v.Remove(key);
                    }
                })
            {
            }

            private static IProcessor GenerateExtraReplace(string id, int iterations, IProcessorState outerProcessor)
            {
                string variableName = $"For-{id}-RunCount";
                int runCount;
                if (!outerProcessor.Config.Variables.TryGetValue(variableName, out object runCountObject))
                {
                    outerProcessor.Config.Variables[variableName] = runCountObject = runCount = 0;
                }
                else
                {
                    runCount = (int)runCountObject;
                }

                if (runCount < iterations && outerProcessor is IOwnedProcessorState ownedState && ownedState.Processor != null)
                {
                    outerProcessor.Config.Variables[variableName] = ++runCount;
                    return ownedState.Processor.CloneAndAppendOperations(new IOperationProvider[] { new Replacement("contents".TokenConfig(), "Hello there!", null, true) });
                }

                return null;
            }

            public override string OperationName => "forloop";
        }
    }
}
