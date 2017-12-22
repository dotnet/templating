using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Core.Operations
{
    public class ExpandVariables : IOperationProvider
    {
        public static readonly string OperationName = "expandvariables";

        private readonly bool _initialState;

        public ExpandVariables(string id, bool initialState)
        {
            Id = id;
            _initialState = initialState;
        }

        public string Id { get; }

        public IOperation GetOperation(Encoding encoding, IProcessorState processor)
        {
            return new Impl(processor, Id, _initialState);
        }

        private class Impl : IOperation
        {
            public Impl(IProcessorState processor, string id, bool initialState)
            {
                Tokens = processor.EncodingConfig.VariableKeys;
                Id = id;
                IsInitialStateOn = string.IsNullOrEmpty(id) || initialState;
            }

            public IReadOnlyList<IToken> Tokens { get; }

            public string Id { get; }

            public bool IsInitialStateOn { get; }

            public int HandleMatch(IProcessorState processor, int bufferLength, ref int currentBufferPosition, int token, Stream target)
            {
                if (processor.Config.Flags.TryGetValue("expandVariables", out bool flag) && !flag)
                {
                    target.Write(Tokens[token].Value, Tokens[token].Start, Tokens[token].Length);
                    return Tokens[token].Length;
                }

                object result = processor.EncodingConfig[token];
                string output = result?.ToString() ?? "null";

                byte[] outputBytes = processor.Encoding.GetBytes(output);
                target.Write(outputBytes, 0, outputBytes.Length);
                return outputBytes.Length;
            }
        }
    }
}
