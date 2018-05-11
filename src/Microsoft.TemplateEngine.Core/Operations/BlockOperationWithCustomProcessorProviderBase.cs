using System;
using System.IO;
using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Core.Operations
{
    public abstract class BlockOperationWithCustomProcessorProviderBase : BlockOperationProviderBase
    {
        private readonly Func<IProcessorState, IProcessor> _getNextProcessor;

        public BlockOperationWithCustomProcessorProviderBase(string id, ITokenConfig startToken, ITokenConfig endToken, bool isInitialStateOn, Func<IProcessorState, IProcessor> getNextProcessor)
            : base(id, startToken, endToken, isInitialStateOn)
        {
            _getNextProcessor = getNextProcessor;
        }

        protected override ImplBase Create(string id, BlockOperationProviderBase owner, ITokenTrie matcher, bool isInitialStateOn)
        {
            return new Impl(id, owner, matcher, isInitialStateOn, _getNextProcessor);
        }

        protected class Impl : ImplBase
        {
            private readonly Func<IProcessorState, IProcessor> _getNextProcessor;

            public Impl(string id, BlockOperationProviderBase owner, ITokenTrie matcher, bool isInitialStateOn, Func<IProcessorState, IProcessor> getNextProcessor)
                : base(id, owner, matcher, isInitialStateOn)
            {
                _getNextProcessor = getNextProcessor;
            }

            protected override int OnBlockIsolated(IProcessorState outerProcessor, Stream blockData, Stream target)
            {
                IProcessor processor = _getNextProcessor(outerProcessor);

                long bytesWrittenMark = target.Position;

                while (processor != null)
                {
                    blockData.Position = 0;
                    processor.Run(blockData, target);
                    processor = _getNextProcessor(outerProcessor);
                }

                return (int)(target.Position - bytesWrittenMark);
            }
        }
    }
}
