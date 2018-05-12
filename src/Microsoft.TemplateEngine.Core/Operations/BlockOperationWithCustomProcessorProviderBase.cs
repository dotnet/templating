using System;
using System.IO;
using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Core.Operations
{
    public abstract class BlockOperationWithCustomProcessorProviderBase : BlockOperationProviderBase
    {
        private readonly Func<IProcessorState, IProcessor> _getNextProcessor;
        private readonly Func<IVariableCollection, object> _establishVariableCollectionBaseline;
        private readonly Action<IVariableCollection, object> _restoreVariableCollection;

        public BlockOperationWithCustomProcessorProviderBase(string id, ITokenConfig startToken, ITokenConfig endToken, bool isInitialStateOn, Func<IProcessorState, IProcessor> getNextProcessor, Func<IVariableCollection, object> establishVariableCollectionBaseline, Action<IVariableCollection, object> restoreVariableCollection)
            : base(id, startToken, endToken, isInitialStateOn)
        {
            _getNextProcessor = getNextProcessor;
            _establishVariableCollectionBaseline = establishVariableCollectionBaseline;
            _restoreVariableCollection = restoreVariableCollection;
        }

        protected override ImplBase Create(string id, BlockOperationProviderBase owner, ITokenTrie matcher, bool isInitialStateOn)
        {
            return new Impl(id, owner, matcher, isInitialStateOn, _getNextProcessor, _establishVariableCollectionBaseline, _restoreVariableCollection);
        }

        protected class Impl : ImplBase
        {
            private readonly Func<IProcessorState, IProcessor> _getNextProcessor;
            private readonly Func<IVariableCollection, object> _establishVariableCollectionBaseline;
            private readonly Action<IVariableCollection, object> _restoreVariableCollection;

            public Impl(string id, BlockOperationProviderBase owner, ITokenTrie matcher, bool isInitialStateOn, Func<IProcessorState, IProcessor> getNextProcessor, Func<IVariableCollection, object> establishVariableCollectionBaseline, Action<IVariableCollection, object> restoreVariableCollection)
                : base(id, owner, matcher, isInitialStateOn)
            {
                _getNextProcessor = getNextProcessor;
                _establishVariableCollectionBaseline = establishVariableCollectionBaseline;
                _restoreVariableCollection = restoreVariableCollection;
            }

            protected override int OnBlockIsolated(IProcessorState outerProcessor, Stream blockData, Stream target)
            {
                object token = _establishVariableCollectionBaseline(outerProcessor.Config.Variables);
                IProcessor processor = _getNextProcessor(outerProcessor);

                long bytesWrittenMark = target.Position;

                while (processor != null)
                {
                    blockData.Position = 0;
                    processor.Run(blockData, target);
                    processor = _getNextProcessor(outerProcessor);
                }

                _restoreVariableCollection(outerProcessor.Config.Variables, token);
                return (int)(target.Position - bytesWrittenMark);
            }
        }
    }
}
