// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;

namespace Microsoft.TemplateEngine.Core.Contracts
{
    public interface IProcessorState
    {
        IEngineConfig Config { get; }

        byte[] CurrentBuffer { get; }

        int CurrentBufferLength { get; }

        int CurrentBufferPosition { get; }

        int CurrentSequenceNumber { get; }

        IEncodingConfig EncodingConfig { get; }

        Encoding Encoding { get; }

        bool AdvanceBuffer(int bufferPosition);

        void SeekBufferForwardUntil(ITokenTrie trie, ref int bufferLength, ref int currentBufferPosition);

        void SeekBufferForwardThrough(ITokenTrie trie, ref int bufferLength, ref int currentBufferPosition);

        void SeekBufferForwardWhile(ITokenTrie trie, ref int bufferLength, ref int currentBufferPosition);

        void SeekTargetBackUntil(ITokenTrie match);

        void SeekTargetBackUntil(ITokenTrie match, bool consume);

        void SeekTargetBackWhile(ITokenTrie match);

        void Write(byte[] buffer, int offset, int count);

        void Inject(Stream staged);
    }
}
