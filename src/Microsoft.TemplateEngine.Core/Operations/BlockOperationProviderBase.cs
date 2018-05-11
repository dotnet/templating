using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Util;

namespace Microsoft.TemplateEngine.Core.Operations
{
    public abstract class BlockOperationProviderBase : IOperationProvider
    {
        private readonly ITokenConfig _endToken;

        private readonly bool _isInitialStateOn;

        private readonly ITokenConfig _startToken;

        protected BlockOperationProviderBase(string id, ITokenConfig startToken, ITokenConfig endToken, bool isInitialStateOn)
        {
            Id = id;
            _startToken = startToken;
            _endToken = endToken;
            _isInitialStateOn = isInitialStateOn;
        }

        public string Id { get; }

        public abstract string OperationName { get; }

        public IOperation GetOperation(Encoding encoding, IProcessorState processorState)
        {
            IToken startTokenBytes = _startToken.ToToken(encoding);
            IToken endTokenBytes = _endToken.ToToken(encoding);
            TokenTrie tokenMatcher = new TokenTrie();
            tokenMatcher.AddToken(startTokenBytes);
            tokenMatcher.AddToken(endTokenBytes);
            return Create(Id, this, tokenMatcher, _isInitialStateOn);
        }

        protected abstract ImplBase Create(string id, BlockOperationProviderBase owner, ITokenTrie matcher, bool isInitialStateOn);

        protected abstract class ImplBase : IOperation
        {
            private readonly ITokenTrie _matcher;

            private readonly BlockOperationProviderBase _owner;

            protected ImplBase(string id, BlockOperationProviderBase owner, ITokenTrie matcher, bool isInitialStateOn)
            {
                _owner = owner;
                Id = id;
                //The processor should only matche the start token
                Tokens = new[] { matcher.Tokens[0] };
                _matcher = matcher;
                IsInitialStateOn = string.IsNullOrEmpty(id) || isInitialStateOn;
            }

            public string Id { get; }

            public bool IsInitialStateOn { get; }

            public IReadOnlyList<IToken> Tokens { get; }

            public int HandleMatch(IProcessorState processor, int bufferLength, ref int currentBufferPosition, int token, Stream target)
            {
                if (processor.Config.Flags.TryGetValue(_owner.OperationName, out bool flag) && !flag)
                {
                    target.Write(Tokens[token].Value, Tokens[token].Start, Tokens[token].Length);
                    return Tokens[token].Length;
                }

                processor.ConsumeWholeLine(ref bufferLength, ref currentBufferPosition);

                //We made it here, so we're already inside a start
                int depth = 1;
                int sequenceNumber = 0;
                ITokenTrieEvaluator evaluator = _matcher.CreateEvaluator();
                MemoryStream blockData = new MemoryStream();

                while (true)
                {
                    if (currentBufferPosition == bufferLength)
                    {
                        if (!processor.AdvanceBuffer(currentBufferPosition))
                        {
                            break;
                        }
                    }

                    //Look to see if we've found a start or end
                    int entrySequenceNumber = sequenceNumber;
                    if (evaluator.Accept(processor.CurrentBuffer[currentBufferPosition], ref sequenceNumber, out int locatedToken))
                    {
                        if (locatedToken == 0)
                        {
                            ++depth;
                        }
                        else
                        {
                            --depth;
                        }

                        if (depth == 0)
                        {
                            sequenceNumber -= _matcher.Tokens[locatedToken].Length;
                        }
                    }

                    blockData.WriteByte(processor.CurrentBuffer[currentBufferPosition]);
                    ++currentBufferPosition;

                    if (depth == 0)
                    {
                        processor.SeekForwardThrough(processor.EncodingConfig.LineEndings, ref bufferLength, ref currentBufferPosition);
                        blockData.SetLength(blockData.Length + sequenceNumber - entrySequenceNumber - 1);
                        SeekBackWhile(processor.EncodingConfig.Whitespace, blockData);
                        blockData.Position = 0;
                        return OnBlockIsolated(processor, blockData, target);
                    }
                    else
                    {
                        ++sequenceNumber;
                    }
                }

                return 0;
            }

            private void SeekBackWhile(ITokenTrie match, Stream target)
            {
                byte[] buffer = new byte[match.MaxLength];
                while (target.Position > 0)
                {
                    if (target.Position < buffer.Length)
                    {
                        target.Position = 0;
                    }
                    else
                    {
                        target.Position -= buffer.Length;
                    }

                    int nRead = target.Read(buffer, 0, buffer.Length);
                    bool anyMatch = false;
                    int token = -1;
                    int i = nRead - match.MinLength;

                    for (; i >= 0; --i)
                    {
                        if (match.GetOperation(buffer, nRead, ref i, out token))
                        {
                            i -= match.TokenLength[token];
                            anyMatch = true;
                            break;
                        }
                    }

                    if (!anyMatch || (token != -1 && i + match.TokenLength[token] != nRead))
                    {
                        target.SetLength(target.Position);
                        return;
                    }

                    //Back up the amount we already read to get a new window of data in
                    if (target.Position < buffer.Length)
                    {
                        target.Position = 0;
                    }
                    else
                    {
                        target.Position -= buffer.Length;
                    }
                }

                if (target.Position == 0)
                {
                    target.SetLength(0);
                }
            }

            protected abstract int OnBlockIsolated(IProcessorState outerProcessor, Stream blockData, Stream target);
        }
    }
}
