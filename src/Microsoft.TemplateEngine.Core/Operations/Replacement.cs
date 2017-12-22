using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Core.Operations
{
    public class Replacement : IOperationProvider
    {
        public static readonly string OperationName = "replacement";

        private readonly ITokenConfig _match;
        private readonly string _replaceWith;
        private readonly bool _initialState;

        public string Id { get; }

        public Replacement(ITokenConfig match, string replaceWith, string id, bool initialState)
        {
            _match = match;
            _replaceWith = replaceWith;
            Id = id;
            _initialState = initialState;
        }

        public IOperation GetOperation(Encoding encoding, IProcessorState processorState)
        {
            IToken token = _match.ToToken(encoding);
            byte[] replaceWith = encoding.GetBytes(_replaceWith);
            
            return token.Value.Skip(token.Start).Take(token.Length).SequenceEqual(replaceWith)
                ? null
                : new Impl(token, replaceWith, Id, _initialState);
        }

        private class Impl : IOperation
        {
            private readonly byte[] _replacement;

            public Impl(IToken token, byte[] replaceWith, string id, bool initialState)
            {
                _replacement = replaceWith;
                Id = id;
                Tokens = new[] {token};
                IsInitialStateOn = string.IsNullOrEmpty(id) || initialState;
            }

            public IReadOnlyList<IToken> Tokens { get; }

            public string Id { get; }

            public bool IsInitialStateOn { get; }

            public int HandleMatch(IProcessorState processor, int bufferLength, ref int currentBufferPosition, int token, Stream target)
            {
                if (processor.Config.Flags.TryGetValue(OperationName, out bool flag) && !flag)
                {
                    target.Write(Tokens[token].Value, Tokens[token].Start, Tokens[token].Length);
                    return Tokens[token].Length;
                }

                target.Write(_replacement, 0, _replacement.Length);
                return _replacement.Length;
            }
        }
    }
}
