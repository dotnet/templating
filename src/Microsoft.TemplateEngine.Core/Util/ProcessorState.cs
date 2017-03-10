using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Matching;

namespace Microsoft.TemplateEngine.Core.Util
{
    public class ProcessorState2 : IProcessorState
    {
        private readonly int _flushThreshold;
        private readonly Stream _source;
        private readonly Stream _target;
        private readonly TrieEvaluator<OperationTerminal> _trie;
        private Encoding _encoding;
        private static readonly ConcurrentDictionary<IReadOnlyList<IOperationProvider>, Dictionary<Encoding, Trie<OperationTerminal>>> TrieLookup = new ConcurrentDictionary<IReadOnlyList<IOperationProvider>, Dictionary<Encoding, Trie<OperationTerminal>>>();

        public ProcessorState2(Stream source, Stream target, int bufferSize, int flushThreshold, IEngineConfig config, IReadOnlyList<IOperationProvider> operationProviders)
        {
            //Buffer has to be at least as large as the largest BOM we could expect
            if (bufferSize < 4)
            {
                bufferSize = 4;
            }
            else
            {
                try
                {
                    if (source.Length < bufferSize)
                    {
                        bufferSize = (int) source.Length;
                    }
                }
                catch
                {
                    //The stream may not support getting the length property (in NetworkStream for instance, which throw a NotSupportedException), suppress any errors in
                    //  accessing the property and continue with the specified buffer size
                }
            }

            _source = source;
            _target = target;
            Config = config;
            _flushThreshold = flushThreshold;
            CurrentBuffer = new byte[bufferSize];
            CurrentBufferLength = source.Read(CurrentBuffer, 0, CurrentBuffer.Length);

            byte[] bom;
            Encoding encoding = EncodingUtil.Detect(CurrentBuffer, CurrentBufferLength, out bom);
            Encoding = encoding;
            CurrentBufferPosition = bom.Length;
            target.Write(bom, 0, bom.Length);

            Dictionary<Encoding, Trie<OperationTerminal>> byEncoding = TrieLookup.GetOrAdd(operationProviders, x => new Dictionary<Encoding, Trie<OperationTerminal>>());

            if (!byEncoding.TryGetValue(encoding, out Trie<OperationTerminal> trie))
            {
                trie = new Trie<OperationTerminal>();

                for (int i = 0; i < operationProviders.Count; ++i)
                {
                    IOperation op = operationProviders[i].GetOperation(encoding, this);

                    if (op != null)
                    {
                        for (int j = 0; j < op.Tokens.Count; ++j)
                        {
                            if (op.Tokens[j] != null)
                            {
                                trie.AddPath(op.Tokens[j].Value, new OperationTerminal(op, j, op.Tokens[j].Value.Length, op.Tokens[j].Start, op.Tokens[j].End));
                            }
                        }
                    }
                }

                byEncoding[encoding] = trie;
            }

            _trie = new TrieEvaluator<OperationTerminal>(trie);

            if (bufferSize < _trie.MaxLength + 1)
            {
                byte[] tmp = new byte[_trie.MaxLength + 1];
                Buffer.BlockCopy(CurrentBuffer, CurrentBufferPosition, tmp, 0, CurrentBufferLength - CurrentBufferPosition);
                int nRead = _source.Read(tmp, CurrentBufferLength - CurrentBufferPosition, tmp.Length - CurrentBufferLength);
                CurrentBuffer = tmp;
                CurrentBufferLength += nRead;
                CurrentBufferPosition = 0;
            }
        }

        public IEngineConfig Config { get; }

        public byte[] CurrentBuffer { get; }

        public int CurrentBufferLength { get; private set; }

        public int CurrentBufferPosition { get; private set; }

        public int CurrentSequenceNumber { get; private set; }

        public Encoding Encoding
        {
            get { return _encoding; }
            set
            {
                _encoding = value;
                EncodingConfig = new EncodingConfig(Config, _encoding);
            }
        }

        public IEncodingConfig EncodingConfig { get; private set; }

        public bool AdvanceBuffer(int bufferPosition)
        {
            if (CurrentBufferLength == 0)
            {
                CurrentBufferPosition = 0;
                return false;
            }

            if(bufferPosition == 0)
            {
                return false;
            }

            //At this point we know that CurrentBufferPosition and CurrentSequenceNumber are related
            //  and we know that CurrentBufferPosition will be set to the head of the buffer at the
            //  end of this method, shifting off bufferPosition bytes. The number of bytes that then
            //  would be advanced in the sequence is then the number of bytes the bufferPosition was
            //  ahead of the CurrentBuffer position
            CurrentSequenceNumber += bufferPosition - CurrentBufferPosition;

            int offset = 0;
            if (bufferPosition != CurrentBufferLength)
            {
                offset = CurrentBufferLength - bufferPosition;
                Array.Copy(CurrentBuffer, bufferPosition, CurrentBuffer, 0, offset);
            }

            int bytesRead = _source.Read(CurrentBuffer, offset, CurrentBuffer.Length - offset);
            CurrentBufferLength = bytesRead + offset;
            CurrentBufferPosition = 0;

            return bytesRead != 0;
        }

        public bool Run()
        {
            bool modified = false;
            int lastWritten = CurrentBufferPosition;
            int writtenSinceFlush = lastWritten;

            if(CurrentBufferPosition == CurrentBufferLength)
            {
                AdvanceBuffer(CurrentBufferPosition);
                lastWritten = 0;
            }

            while (CurrentBufferLength > 0)
            {
                int token;
                int posedPosition = CurrentBufferPosition;

                if (CurrentBufferLength == CurrentBuffer.Length && CurrentBufferLength == CurrentBufferPosition)
                {
                    int writeCount = CurrentBufferLength - lastWritten;

                    if (writeCount > 0)
                    {
                        _target.Write(CurrentBuffer, lastWritten, writeCount);
                        writtenSinceFlush += writeCount;
                    }

                    AdvanceBuffer(CurrentBufferLength - (CurrentSequenceNumber - _trie.OldestRequiredSequenceNumber));
                    lastWritten = 0;
                    posedPosition = 0;
                }

                int sn = CurrentSequenceNumber;
                bool isMatch = _trie.Accept(CurrentBuffer[CurrentBufferPosition], ref sn, out TerminalLocation<OperationTerminal> terminal);
                IOperation op = isMatch ? terminal.Terminal.Operation : null;
                sn = isMatch ? terminal.Location + terminal.Terminal.End - terminal.Terminal.Start : sn;
                CurrentBufferPosition -= CurrentSequenceNumber - sn;
                CurrentSequenceNumber = sn;
                token = isMatch ? terminal.Terminal.Token : -1;
                bool opEnabledFlag;

                if ((op != null)
                        && ((op.Id == null)
                            || (Config.Flags.TryGetValue(op.Id, out opEnabledFlag) && opEnabledFlag))
                    )
                {
                    // The operation will be processed because one of these conditions are met:
                    // - The operation doesn't have an id (thus can't be disabled)
                    // - The flag for the Id exists and is true.

                    int writeCount = CurrentBufferPosition - (CurrentSequenceNumber - terminal.Location) - lastWritten;

                    if (writeCount > 0)
                    {
                        _target.Write(CurrentBuffer, lastWritten, writeCount);
                        writtenSinceFlush += writeCount;
                    }

                    //Advance the sequence number by the number of bytes taken by the token
                    CurrentSequenceNumber += posedPosition - CurrentBufferPosition;
                    CurrentBufferPosition = posedPosition;

                    try
                    {
                        writtenSinceFlush += op.HandleMatch(this, CurrentBufferLength, ref posedPosition, token, _target);
                        CurrentSequenceNumber += posedPosition - CurrentBufferPosition;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error running handler {op} at position {CurrentBufferPosition} in {Encoding.EncodingName} bytes of {Encoding.GetString(CurrentBuffer, 0, CurrentBufferLength)}.\n\nStart: {Encoding.GetString(CurrentBuffer, CurrentBufferPosition, CurrentBufferLength - CurrentBufferPosition)} \n\nCheck InnerException for details.", ex);
                    }

                    CurrentBufferPosition = posedPosition;
                    lastWritten = posedPosition;
                    modified = true;
                }
                else
                {
                    // If the operation is disabled, it's as if the token were just arbitrary text, so do nothing special with it.
                    ++CurrentBufferPosition;
                    ++CurrentSequenceNumber;
                }

                if (CurrentBufferPosition == CurrentBufferLength)
                {
                    int writeCount = CurrentBufferPosition - lastWritten - (CurrentSequenceNumber - _trie.OldestRequiredSequenceNumber);

                    if (writeCount > 0)
                    {
                        _target.Write(CurrentBuffer, lastWritten, writeCount);
                        writtenSinceFlush += writeCount;
                    }

                    int advanceBufferMax = CurrentBufferLength - (CurrentSequenceNumber - _trie.OldestRequiredSequenceNumber);
                    if (!AdvanceBuffer(Math.Min(CurrentBufferPosition, Math.Max(0, advanceBufferMax))) && CurrentBufferPosition == CurrentBufferLength)
                    {
                        break;
                    }

                    lastWritten = 0;
                }

                if (writtenSinceFlush >= _flushThreshold)
                {
                    writtenSinceFlush = 0;
                    _target.Flush();
                }
            }

            int n = CurrentSequenceNumber;
            _trie.FinalizeMatchesInProgress(ref n, out TerminalLocation<OperationTerminal> term);

            if(term != null)
            {
                IOperation op = term.Terminal.Operation;
                int sn = term.Location + term.Terminal.End - term.Terminal.Start;
                CurrentBufferPosition -= CurrentSequenceNumber - sn;
                CurrentSequenceNumber = sn;
                bool opEnabledFlag;

                if ((op != null)
                        && ((op.Id == null)
                            || (Config.Flags.TryGetValue(op.Id, out opEnabledFlag) && opEnabledFlag))
                    )
                {
                    int pos = CurrentBufferPosition;
                    op.HandleMatch(this, CurrentBufferLength, ref pos, term.Terminal.Token, _target);
                    lastWritten = pos;
                    _target.Flush();
                    modified = true;
                }
            }

            if (lastWritten < CurrentBufferPosition)
            {
                int writeCount = CurrentBufferPosition - lastWritten;

                if (writeCount > 0)
                {
                    _target.Write(CurrentBuffer, lastWritten, writeCount);
                }
            }

            _target.Flush();
            return modified;
        }

        public void SeekBackUntil(ITokenTrie match)
        {
            SeekBackUntil(match, false);
        }

        public void SeekBackUntil(ITokenTrie match, bool consume)
        {
            byte[] buffer = new byte[match.MaxLength];
            while (_target.Position > 0)
            {
                if (_target.Position < buffer.Length)
                {
                    _target.Position = 0;
                }
                else
                {
                    _target.Position -= buffer.Length;
                }

                int nRead = _target.Read(buffer, 0, buffer.Length);
                int best = -1;
                int bestPos = -1;
                for (int i = nRead - match.MinLength; i >= 0; --i)
                {
                    int token;
                    int ic = i;
                    if (match.GetOperation(buffer, nRead, ref ic, out token) && ic >= bestPos)
                    {
                        bestPos = ic;
                        best = token;
                    }
                }

                if (best != -1)
                {
                    _target.Position -= nRead - bestPos + (consume ? match.TokenLength[best] : 0);
                    _target.SetLength(_target.Position);
                    return;
                }

                //Back up the amount we already read to get a new window of data in
                if (_target.Position < buffer.Length)
                {
                    _target.Position = 0;
                }
                else
                {
                    _target.Position -= buffer.Length;
                }
            }

            if (_target.Position == 0)
            {
                _target.SetLength(0);
            }
        }

        public void SeekBackWhile(ITokenTrie match)
        {
            byte[] buffer = new byte[match.MaxLength];
            while (_target.Position > 0)
            {
                if (_target.Position < buffer.Length)
                {
                    _target.Position = 0;
                }
                else
                {
                    _target.Position -= buffer.Length;
                }

                int nRead = _target.Read(buffer, 0, buffer.Length);
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
                    _target.SetLength(_target.Position);
                    return;
                }

                //Back up the amount we already read to get a new window of data in
                if (_target.Position < buffer.Length)
                {
                    _target.Position = 0;
                }
                else
                {
                    _target.Position -= buffer.Length;
                }
            }

            if (_target.Position == 0)
            {
                _target.SetLength(0);
            }
        }

        public void SeekForwardThrough(ITokenTrie match, ref int bufferLength, ref int currentBufferPosition)
        {
            BaseSeekForward(match, ref bufferLength, ref currentBufferPosition, true);
        }

        public void SeekForwardUntil(ITokenTrie match, ref int bufferLength, ref int currentBufferPosition)
        {
            BaseSeekForward(match, ref bufferLength, ref currentBufferPosition, false);
        }

        private void BaseSeekForward(ITokenTrie match, ref int bufferLength, ref int currentBufferPosition, bool consumeToken)
        {
            while (bufferLength >= match.MinLength)
            {
                //Try to get at least the max length of the tree into the buffer
                if (bufferLength - currentBufferPosition < match.MaxLength)
                {
                    AdvanceBuffer(currentBufferPosition);
                    currentBufferPosition = CurrentBufferPosition;
                    bufferLength = CurrentBufferLength;
                }

                int sz = bufferLength == CurrentBuffer.Length ? match.MaxLength : match.MinLength;

                for (; currentBufferPosition < bufferLength - sz + 1; ++currentBufferPosition)
                {
                    if (bufferLength == 0)
                    {
                        currentBufferPosition = 0;
                        return;
                    }

                    int token;
                    if (match.GetOperation(CurrentBuffer, bufferLength, ref currentBufferPosition, out token))
                    {
                        if (!consumeToken)
                        {
                            currentBufferPosition -= match.Tokens[token].Length;
                        }

                        return;
                    }
                }
            }

            //Ran out of places to check and haven't reached the actual match, consume all the way to the end
            currentBufferPosition = bufferLength;
        }

        public void SeekForwardWhile(ITokenTrie match, ref int bufferLength, ref int currentBufferPosition)
        {
            while (bufferLength > match.MinLength)
            {
                while (currentBufferPosition < bufferLength - match.MinLength + 1)
                {
                    if (bufferLength == 0)
                    {
                        currentBufferPosition = 0;
                        return;
                    }

                    int token;
                    if (!match.GetOperation(CurrentBuffer, bufferLength, ref currentBufferPosition, out token))
                    {
                        return;
                    }
                }

                AdvanceBuffer(currentBufferPosition);
                currentBufferPosition = CurrentBufferPosition;
                bufferLength = CurrentBufferLength;
            }
        }
    }
}
