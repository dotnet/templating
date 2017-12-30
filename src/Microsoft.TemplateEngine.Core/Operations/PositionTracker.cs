using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Core.Operations
{
    public class PositionTracker : IOperationProvider
    {
        public static readonly string OperationName = "track";
        private readonly bool _isInitialStateOn;

        public string Id { get; }

        public ITokenConfig Token { get; }

        public PositionTracker(string id, ITokenConfig token, bool isInitialStateOn)
        {
            Id = id;
            Token = token;
            _isInitialStateOn = isInitialStateOn;
        }

        public IOperation GetOperation(Encoding encoding, IProcessorState processorState)
        {
            IToken[] tokens =
            {
                Token.ToToken(encoding),
            };

            return new Impl(tokens, Id, _isInitialStateOn);
        }

        private class Impl : IOperation
        {
            public Impl(IReadOnlyList<IToken> tokens, string id, bool isInitialStateOn)
            {
                Tokens = tokens;
                Id = id;
                IsInitialStateOn = isInitialStateOn;
            }

            public IReadOnlyList<IToken> Tokens { get; }

            public int HandleMatch(IProcessorState processor, int bufferLength, ref int currentBufferPosition, int token, Stream target)
            {
                if (processor is IOutputValuesContainerAccessor accessor && target.CanSeek)
                {
                    accessor.SetValue(Id, target.Position);
                }

                return 0;
            }

            public string Id { get; }

            public bool IsInitialStateOn { get; }
        }
    }
}
