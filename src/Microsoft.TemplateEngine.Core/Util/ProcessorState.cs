// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Matching;

namespace Microsoft.TemplateEngine.Core.Util
{
    public class ProcessorState : IProcessorState
    {
        private static readonly ConcurrentDictionary<IReadOnlyList<IOperationProvider>, Dictionary<Encoding, Trie<OperationTerminal>>> TrieLookup = new();
        private static readonly ConcurrentDictionary<IReadOnlyList<IOperationProvider>, List<string>> OperationsToExplicitlySetOnByDefault = new();
        private readonly StreamProxy _target;
        private readonly TrieEvaluator<OperationTerminal> _trie;
        private readonly int _flushThreshold;
        private readonly int _bomSize;
        private readonly ILogger _logger;
        private Stream _source;

        public ProcessorState(Stream source, Stream target, int bufferSize, int flushThreshold, IEngineConfig config, IReadOnlyList<IOperationProvider> operationProviders)
        {
            _logger = config.Logger;
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (operationProviders == null)
            {
                throw new ArgumentNullException(nameof(operationProviders));
            }

            if (source.CanSeek)
            {
                try
                {
                    if (source.Length < bufferSize)
                    {
                        bufferSize = (int)source.Length;
                    }
                }
                catch
                {
                    //The stream may not support getting the length property (in NetworkStream for instance, which throw a NotSupportedException), suppress any errors in
                    //  accessing the property and continue with the specified buffer size
                }
            }
            //Buffer has to be at least as large as the largest BOM we could expect
            else if (bufferSize < 4)
            {
                bufferSize = 4;
            }
            _logger.LogDebug("Buffer size: {0}", bufferSize);

            _source = source;
            _target = new StreamProxy(target, bufferSize);
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _flushThreshold = flushThreshold;
            CurrentBuffer = new byte[bufferSize];
            CurrentBufferLength = ProcessorState.ReadExactBytes(source, CurrentBuffer, 0, CurrentBuffer.Length);

            Encoding encoding = EncodingUtil.Detect(CurrentBuffer, CurrentBufferLength, out byte[] bom);
            EncodingConfig = new EncodingConfig(Config, encoding);
            _bomSize = bom.Length;
            CurrentBufferPosition = _bomSize;
            CurrentSequenceNumber = _bomSize;
            Write(bom, 0, _bomSize);
            _logger.LogTrace("Writing BOM", Encoding.GetString(bom, 0, _bomSize));
            _logger.LogTrace("{0}: {1}", nameof(CurrentBufferLength), CurrentBufferLength);
            _logger.LogTrace("{0}: {1}", nameof(CurrentBufferPosition), CurrentBufferPosition);
            _logger.LogTrace("buffer is: {0}", Encoding.GetString(CurrentBuffer, 0, CurrentBufferLength));
            _logger.LogTrace("buffer is: {0}", string.Join(" ", CurrentBuffer.Take(CurrentBufferLength)));
            _logger.LogTrace("{0}: {1}", nameof(CurrentBufferLength), CurrentBufferLength);

            bool explicitOnConfigurationRequired = false;
            Dictionary<Encoding, Trie<OperationTerminal>> byEncoding = TrieLookup.GetOrAdd(operationProviders, x => new Dictionary<Encoding, Trie<OperationTerminal>>());
            List<string> turnOnByDefault = OperationsToExplicitlySetOnByDefault.GetOrAdd(operationProviders, x =>
            {
                explicitOnConfigurationRequired = true;
                return new List<string>();
            });

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
                            IToken? currentToken = op.Tokens[j];
                            if (currentToken != null)
                            {
                                //token index makes sense only in concrete IOperation implementation
                                //index will be passed to HandleMatch so IOperation knows which token was matched
                                trie.AddPath(currentToken.Value, new OperationTerminal(op, j, currentToken.Length, currentToken.Start, currentToken.End));
                            }
                        }

                        if (explicitOnConfigurationRequired && op.IsInitialStateOn && !string.IsNullOrEmpty(op.Id))
                        {
                            turnOnByDefault.Add(op.Id!);
                        }
                    }
                }

                byEncoding[encoding] = trie;
            }

            foreach (string state in turnOnByDefault)
            {
                config.Flags[state] = true;
            }

            _trie = new TrieEvaluator<OperationTerminal>(trie);

            if (bufferSize < _trie.MaxLength + 1)
            {
                _logger.LogTrace("Updating buffer to trie max length: {0}", _trie.MaxLength + 1);
                byte[] tmp = new byte[_trie.MaxLength + 1];
                Buffer.BlockCopy(CurrentBuffer, CurrentBufferPosition, tmp, 0, CurrentBufferLength - CurrentBufferPosition);
                int nRead = ProcessorState.ReadExactBytes(_source, tmp, CurrentBufferLength - CurrentBufferPosition, tmp.Length - CurrentBufferLength);
                CurrentBuffer = tmp;
                CurrentBufferLength += nRead - _bomSize;
                CurrentBufferPosition = 0;
                CurrentSequenceNumber = 0;

                _logger.LogTrace("buffer is: {0}", Encoding.GetString(CurrentBuffer, 0, CurrentBufferLength));
                _logger.LogTrace("{0}: {1}", nameof(CurrentBufferLength), CurrentBufferLength);
                _logger.LogTrace("{0}: {1}", nameof(CurrentBufferPosition), CurrentBufferPosition);
            }
        }

        public IEngineConfig Config { get; }

        public byte[] CurrentBuffer { get; }

        /// <summary>
        /// Current buffer length. May be less the length of <see cref="CurrentBuffer"/>.
        /// </summary>
        public int CurrentBufferLength { get; private set; }

        /// <summary>
        /// Current buffer position.
        /// </summary>
        public int CurrentBufferPosition { get; private set; }

        /// <summary>
        /// Next position to be processed. Counted from 0.
        /// If <see cref="CurrentBufferPosition"/> is a position within the buffer, the <see cref="CurrentSequenceNumber"/> counts from 0.
        /// </summary>
        public int CurrentSequenceNumber { get; private set; }

        public IEncodingConfig EncodingConfig { get; }

        public Encoding Encoding => EncodingConfig.Encoding;

        public bool AdvanceBuffer(int bufferPosition)
        {
            _logger.LogTrace("{0}, {1}: {2}", nameof(AdvanceBuffer), nameof(bufferPosition), bufferPosition);
            _logger.LogTrace("{0}: {1}", nameof(CurrentBufferPosition), CurrentBufferPosition);
            _logger.LogTrace("{0}: {1}", nameof(CurrentBufferLength), CurrentBufferLength);
            _logger.LogTrace("{0}: {1}", nameof(CurrentSequenceNumber), CurrentSequenceNumber);
            if (CurrentBufferLength == 0 || bufferPosition == 0)
            {
                return false;
            }

            //The number of bytes away from the current buffer position being
            //  retargeted to the buffer head
            int netMove = bufferPosition - CurrentBufferPosition;
            _logger.LogTrace("{0}: {1}", nameof(netMove), netMove);
            //Since the CurrentSequenceNumber and CurrentBufferPosition are
            //  different mappings over the same value, the same net move
            //  applies to the current sequence number
            CurrentSequenceNumber += netMove;
            _logger.LogTrace("updated {0}: {1}", nameof(CurrentSequenceNumber), CurrentSequenceNumber);
            //Calculate the number of bytes at the end of the buffer that
            //  should be preserved
            int bytesToPreserveInBuffer = CurrentBufferLength - bufferPosition;
            _logger.LogTrace("{0}: {1}", nameof(bytesToPreserveInBuffer), bytesToPreserveInBuffer);

            if (CurrentBufferLength < CurrentBuffer.Length && bytesToPreserveInBuffer == 0)
            {
                // CurrentBufferLength < CurrentBuffer.Length means we already at the end at the previous iteration
                // there were not enough data to fill the whole buffer before
                // advancing will not read additional information.
                CurrentBufferLength = 0;
                CurrentBufferPosition = 0;
                return false;
            }

            //If we actually have to preserve any data, shift it to the start
            if (bytesToPreserveInBuffer > 0)
            {
                //Shift the relevant number of bytes back to the head of the buffer
                Buffer.BlockCopy(CurrentBuffer, bufferPosition, CurrentBuffer, 0, bytesToPreserveInBuffer);
            }

            //Fill the remaining spaces in the buffer with new data, save how
            //  many we've read for recalculating the new effective buffer size
            int nRead = ProcessorState.ReadExactBytes(_source, CurrentBuffer, bytesToPreserveInBuffer, CurrentBufferLength - bytesToPreserveInBuffer);
            _logger.LogTrace("{0}: {1}", nameof(nRead), nRead);
            CurrentBufferLength = bytesToPreserveInBuffer + nRead;
            _logger.LogTrace("updated {0}: {1}", nameof(CurrentBufferLength), CurrentBufferLength);
            _logger.LogTrace("buffer is: {0}", Encoding.GetString(CurrentBuffer, 0, CurrentBufferLength));
            //The new buffer position is set to point at the byte that buffer
            //  position pointed at (which is now at the head of the buffer)
            CurrentBufferPosition = 0;

            return true;
        }

        public bool Run()
        {
            int nextSequenceNumberThatCouldBeWritten = CurrentSequenceNumber;
            int bytesWrittenSinceLastFlush = 0;
            bool anyOperationsExecuted = false;

            while (true)
            {
                //Loop until we run out of data in the buffer
                while (CurrentBufferPosition < CurrentBufferLength)
                {
                    int posedPosition = CurrentSequenceNumber;
                    bool skipAdvanceBuffer = false;
                    if (_trie.Accept(CurrentBuffer[CurrentBufferPosition], ref posedPosition, out TerminalLocation<OperationTerminal> terminal))
                    {
                        IOperation operation = terminal.Terminal.Operation;
                        int matchLength = terminal.Terminal.End - terminal.Terminal.Start + 1;
                        int handoffBufferPosition = CurrentBufferPosition + matchLength - (CurrentSequenceNumber - terminal.Location);

                        _logger.LogTrace("Found operation: {0}", terminal.Terminal.Operation.Id);
                        _logger.LogTrace("{0}: {1}", nameof(CurrentBufferPosition), CurrentBufferPosition);
                        _logger.LogTrace("{0}: {1}", nameof(CurrentSequenceNumber), CurrentSequenceNumber);
                        _logger.LogTrace("{0}: {1}", nameof(posedPosition), posedPosition);
                        _logger.LogTrace("{0}: {1}", nameof(matchLength), matchLength);
                        _logger.LogTrace("{0}: {1}", nameof(handoffBufferPosition), handoffBufferPosition);

                        if (terminal.Location > nextSequenceNumberThatCouldBeWritten)
                        {
                            int toWrite = terminal.Location - nextSequenceNumberThatCouldBeWritten;
                            //Console.WriteLine("UnmatchedBlock");
                            //string text = System.Text.Encoding.UTF8.GetString(CurrentBuffer, handoffBufferPosition - toWrite - matchLength, toWrite).Replace("\0", "\\0");
                            //Console.WriteLine(text);
                            _target.Write(CurrentBuffer, handoffBufferPosition - toWrite - matchLength, toWrite);
                            _logger.LogTrace("Written {0} bytes before operation processing", toWrite);
                            bytesWrittenSinceLastFlush += toWrite;
                            nextSequenceNumberThatCouldBeWritten = posedPosition - matchLength + 1;
                        }

                        if (operation.Id == null || (Config.Flags.TryGetValue(operation.Id, out bool opEnabledFlag) && opEnabledFlag))
                        {
                            _logger.LogTrace("Running operation");
                            CurrentSequenceNumber += handoffBufferPosition - CurrentBufferPosition;
                            CurrentBufferPosition = handoffBufferPosition;
                            _logger.LogTrace("{0}: {1}", nameof(CurrentSequenceNumber), CurrentSequenceNumber);
                            _logger.LogTrace("{0}: {1}", nameof(CurrentBufferPosition), CurrentBufferPosition);
                            posedPosition = handoffBufferPosition;
                            int bytesWritten = operation.HandleMatch(this, CurrentBufferLength, ref posedPosition, terminal.Terminal.Token);
                            bytesWrittenSinceLastFlush += bytesWritten;

                            _logger.LogTrace("Written {0} bytes after operation processing", bytesWritten);
                            CurrentSequenceNumber += posedPosition - CurrentBufferPosition;
                            CurrentBufferPosition = posedPosition;
                            _logger.LogTrace("{0}: {1}", nameof(CurrentSequenceNumber), CurrentSequenceNumber);
                            _logger.LogTrace("{0}: {1}", nameof(CurrentBufferPosition), CurrentBufferPosition);
                            nextSequenceNumberThatCouldBeWritten = CurrentSequenceNumber;
                            skipAdvanceBuffer = true;
                            anyOperationsExecuted = true;
                        }
                        else
                        {
                            int oldSequenceNumber = CurrentSequenceNumber;
                            CurrentSequenceNumber = terminal.Location + terminal.Terminal.End + 1;
                            CurrentBufferPosition += CurrentSequenceNumber - oldSequenceNumber;
                            _logger.LogTrace("Operation is disabled.");
                            _logger.LogTrace("{0}: {1}", nameof(CurrentSequenceNumber), CurrentSequenceNumber);
                            _logger.LogTrace("{0}: {1}", nameof(CurrentBufferPosition), CurrentBufferPosition);
                        }

                        if (bytesWrittenSinceLastFlush >= _flushThreshold)
                        {
                            _target.Flush();
                            bytesWrittenSinceLastFlush = 0;
                        }
                    }

                    if (!skipAdvanceBuffer)
                    {
                        ++CurrentSequenceNumber;
                        ++CurrentBufferPosition;
                    }
                }

                //Calculate the sequence number at the head of the buffer
                int headSequenceNumber = CurrentSequenceNumber - CurrentBufferPosition;

                // Calculate the buffer position to advance to. It can not be negative.
                // Taking a maximum is a workaround for out-of-sync _trie.OldestRequiredSequenceNumber which may appear near EOF.
                int bufferPositionToAdvanceTo;
                if (headSequenceNumber > _trie.OldestRequiredSequenceNumber)
                {
                    // if headSequenceNumber is higher than _trie.OldestRequiredSequenceNumber
                    // the window is already missed
                    // we won't be able to continue with current tries anyway
                    // advance to new chunk of the buffer.
                    bufferPositionToAdvanceTo = CurrentBufferLength;
                }
                else
                {
                    bufferPositionToAdvanceTo = _trie.OldestRequiredSequenceNumber - headSequenceNumber;
                }
                int numberOfUncommittedBytesBeforeThePositionToAdvanceTo = _trie.OldestRequiredSequenceNumber - nextSequenceNumberThatCouldBeWritten;

                _logger.LogTrace("{0}: {1}", nameof(headSequenceNumber), headSequenceNumber);
                _logger.LogTrace("{0}: {1}", nameof(_trie.OldestRequiredSequenceNumber), _trie.OldestRequiredSequenceNumber);
                _logger.LogTrace("{0}: {1}", nameof(bufferPositionToAdvanceTo), bufferPositionToAdvanceTo);
                _logger.LogTrace("{0}: {1}", nameof(numberOfUncommittedBytesBeforeThePositionToAdvanceTo), numberOfUncommittedBytesBeforeThePositionToAdvanceTo);

                //If we'd advance data out of the buffer that hasn't been
                //  handled already, write it out
                if (numberOfUncommittedBytesBeforeThePositionToAdvanceTo > 0)
                {
                    int toWrite = numberOfUncommittedBytesBeforeThePositionToAdvanceTo;
                    // Console.WriteLine("AdvancePreserve");
                    // Console.WriteLine($"nextSequenceNumberThatCouldBeWritten {nextSequenceNumberThatCouldBeWritten}");
                    // Console.WriteLine($"headSequenceNumber {headSequenceNumber}");
                    // Console.WriteLine($"bufferPositionToAdvanceTo {bufferPositionToAdvanceTo}");
                    // Console.WriteLine($"numberOfUncommittedBytesBeforeThePositionToAdvanceTo {numberOfUncommittedBytesBeforeThePositionToAdvanceTo}");
                    // Console.WriteLine($"CurrentBufferPosition {CurrentBufferPosition}");
                    // Console.WriteLine($"CurrentBufferLength {CurrentBufferLength}");
                    // Console.WriteLine($"CurrentBuffer.Length {CurrentBuffer.Length}");
                    // string text = System.Text.Encoding.UTF8.GetString(CurrentBuffer, bufferPositionToAdvanceTo - toWrite, toWrite).Replace("\0", "\\0");
                    // Console.WriteLine(text);
                    _target.Write(CurrentBuffer, bufferPositionToAdvanceTo - toWrite, toWrite);
                    bytesWrittenSinceLastFlush += toWrite;
                    nextSequenceNumberThatCouldBeWritten = _trie.OldestRequiredSequenceNumber;
                }

                //We ran out of data in the buffer, so attempt to advance
                //  if we fail,
                _logger.LogTrace("Advancing buffer to: {0}", bufferPositionToAdvanceTo);
                if (!AdvanceBuffer(bufferPositionToAdvanceTo))
                {
                    int posedPosition = CurrentSequenceNumber;
                    _trie.FinalizeMatchesInProgress(ref posedPosition, out TerminalLocation<OperationTerminal> terminal);

                    while (terminal != null)
                    {
                        _logger.LogTrace("Finalizing operations");
                        IOperation operation = terminal.Terminal.Operation;
                        int matchLength = terminal.Terminal.End - terminal.Terminal.Start + 1;
                        int handoffBufferPosition = CurrentBufferPosition + matchLength - (CurrentSequenceNumber - terminal.Location);
                        _logger.LogTrace("Found operation: {0}", terminal.Terminal.Operation.Id);
                        _logger.LogTrace("{0}: {1}", nameof(CurrentBufferPosition), CurrentBufferPosition);
                        _logger.LogTrace("{0}: {1}", nameof(posedPosition), posedPosition);
                        _logger.LogTrace("{0}: {1}", nameof(matchLength), matchLength);
                        _logger.LogTrace("{0}: {1}", nameof(handoffBufferPosition), handoffBufferPosition);

                        if (terminal.Location > nextSequenceNumberThatCouldBeWritten)
                        {
                            int toWrite = terminal.Location - nextSequenceNumberThatCouldBeWritten;
                            // Console.WriteLine("TailUnmatchedBlock");
                            // string text = System.Text.Encoding.UTF8.GetString(CurrentBuffer, handoffBufferPosition - toWrite - matchLength, toWrite).Replace("\0", "\\0");
                            // Console.WriteLine(text);
                            _target.Write(CurrentBuffer, handoffBufferPosition - toWrite - matchLength, toWrite);
                            _logger.LogTrace("Written {0} bytes before operation processing", toWrite);
                            bytesWrittenSinceLastFlush += toWrite;
                            nextSequenceNumberThatCouldBeWritten = terminal.Location;
                        }

                        if (operation.Id == null || (Config.Flags.TryGetValue(operation.Id, out bool opEnabledFlag) && opEnabledFlag))
                        {
                            _logger.LogTrace("Running operation");
                            CurrentSequenceNumber += handoffBufferPosition - CurrentBufferPosition;
                            CurrentBufferPosition = handoffBufferPosition;
                            posedPosition = handoffBufferPosition;
                            _logger.LogTrace("{0}: {1}", nameof(CurrentSequenceNumber), CurrentSequenceNumber);
                            _logger.LogTrace("{0}: {1}", nameof(CurrentBufferPosition), CurrentBufferPosition);
                            int bytesWritten = operation.HandleMatch(this, CurrentBufferLength, ref posedPosition, terminal.Terminal.Token);
                            bytesWrittenSinceLastFlush += bytesWritten;
                            _logger.LogTrace("Written {0} bytes after operation processing", bytesWritten);
                            CurrentSequenceNumber += posedPosition - CurrentBufferPosition;
                            CurrentBufferPosition = posedPosition;
                            _logger.LogTrace("{0}: {1}", nameof(CurrentSequenceNumber), CurrentSequenceNumber);
                            _logger.LogTrace("{0}: {1}", nameof(CurrentBufferPosition), CurrentBufferPosition);
                            nextSequenceNumberThatCouldBeWritten = CurrentSequenceNumber;
                            anyOperationsExecuted = true;
                        }
                        else
                        {
                            int oldSequenceNumber = CurrentSequenceNumber;
                            CurrentSequenceNumber = terminal.Location + terminal.Terminal.End + 1;
                            CurrentBufferPosition += CurrentSequenceNumber - oldSequenceNumber;
                            _logger.LogTrace("Operation ID is not set.");
                            _logger.LogTrace("{0}: {1}", nameof(CurrentSequenceNumber), CurrentSequenceNumber);
                            _logger.LogTrace("{0}: {1}", nameof(CurrentBufferPosition), CurrentBufferPosition);
                        }

                        _trie.FinalizeMatchesInProgress(ref posedPosition, out terminal);
                    }

                    break;
                }
            }

            int endSequenceNumber = CurrentSequenceNumber - CurrentBufferPosition + CurrentBufferLength;
            if (endSequenceNumber > nextSequenceNumberThatCouldBeWritten)
            {
                int toWrite = endSequenceNumber - nextSequenceNumberThatCouldBeWritten;
                // Console.WriteLine("LastBlock");
                // string text = System.Text.Encoding.UTF8.GetString(CurrentBuffer, CurrentBufferLength - toWrite, toWrite).Replace("\0", "\\0");
                // Console.WriteLine(text);
                _target.Write(CurrentBuffer, CurrentBufferLength - toWrite, toWrite);
            }

            _target.FlushToTarget();
            return anyOperationsExecuted;
        }

        public void SeekTargetBackUntil(ITokenTrie match)
        {
            SeekTargetBackUntil(match, false);
        }

        public void SeekTargetBackUntil(ITokenTrie match, bool consume)
        {
            byte[] buffer = new byte[match.MaxLength];
            while (_target.Position > _bomSize)
            {
                if (_target.Position - _bomSize < buffer.Length)
                {
                    _target.Position = _bomSize;
                }
                else
                {
                    _target.Position -= buffer.Length;
                }

                int nRead = ProcessorState.ReadExactBytes(_target, buffer, 0, buffer.Length);

                int best = -1;
                int bestPos = -1;
                for (int i = nRead - match.MinLength; i >= 0; --i)
                {
                    int ic = i;
                    if (match.GetOperation(buffer, nRead, ref ic, out int token) && ic >= bestPos)
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
                if (_target.Position - _bomSize < buffer.Length)
                {
                    _target.Position = _bomSize;
                }
                else
                {
                    _target.Position -= buffer.Length;
                }
            }

            if (_target.Position == _bomSize)
            {
                _target.SetLength(_bomSize);
            }
        }

        public void SeekTargetBackWhile(ITokenTrie match)
        {
            byte[] buffer = new byte[match.MaxLength];
            while (_target.Position > _bomSize)
            {
                if (_target.Position - _bomSize < buffer.Length)
                {
                    _target.Position = _bomSize;
                }
                else
                {
                    _target.Position -= buffer.Length;
                }

                int nRead = ProcessorState.ReadExactBytes(_target, buffer, 0, buffer.Length);
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
                if (_target.Position - _bomSize < buffer.Length)
                {
                    _target.Position = _bomSize;
                }
                else
                {
                    _target.Position -= buffer.Length;
                }
            }

            if (_target.Position == _bomSize)
            {
                _target.SetLength(_bomSize);
            }
        }

        public void Write(byte[] buffer, int offset, int count) => _target.Write(buffer, offset, count);

        public void SeekBufferForwardThrough(ITokenTrie trie, ref int bufferLength, ref int currentBufferPosition)
        {
            BaseSeekBufferForward(trie, ref bufferLength, ref currentBufferPosition, true);
        }

        public void SeekBufferForwardUntil(ITokenTrie trie, ref int bufferLength, ref int currentBufferPosition)
        {
            BaseSeekBufferForward(trie, ref bufferLength, ref currentBufferPosition, false);
        }

        public void SeekBufferForwardWhile(ITokenTrie trie, ref int bufferLength, ref int currentBufferPosition)
        {
            while (bufferLength > trie.MinLength)
            {
                while (currentBufferPosition < bufferLength - trie.MinLength + 1)
                {
                    if (bufferLength == 0)
                    {
                        currentBufferPosition = 0;
                        return;
                    }
                    if (!trie.GetOperation(CurrentBuffer, bufferLength, ref currentBufferPosition, out _))
                    {
                        return;
                    }
                }

                AdvanceBuffer(currentBufferPosition);
                currentBufferPosition = CurrentBufferPosition;
                bufferLength = CurrentBufferLength;
            }
        }

        public void Inject(Stream staged)
        {
            _source = new CombinedStream(staged, _source, inner => _source = inner);
            CurrentBufferLength = ProcessorState.ReadExactBytes(_source, CurrentBuffer, 0, CurrentBufferLength);
            CurrentBufferPosition = 0;
        }

        private static int ReadExactBytes(Stream stream, byte[] buffer, int offset, int count)
        {
            if (count + offset > buffer.Length)
            {
                //cannot read more than available buffer length
                count = buffer.Length - offset;
            }
            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = stream.Read(buffer, totalRead + offset, count - totalRead);
                if (bytesRead == 0)
                {
                    return totalRead;
                }
                totalRead += bytesRead;
            }
            return totalRead;
        }

        private void BaseSeekBufferForward(ITokenTrie match, ref int bufferLength, ref int currentBufferPosition, bool consumeToken)
        {
            _logger.LogTrace("{0}, {1}: {2}, {3}: {4}, {5}: {6}", nameof(BaseSeekBufferForward), nameof(bufferLength), bufferLength, nameof(currentBufferPosition), currentBufferPosition, nameof(consumeToken), consumeToken);
            _logger.LogTrace("Tokens that can be matched: {0}.", string.Join(" ", match.Tokens.Select(t => t.Value)));
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

                    if (match.GetOperation(CurrentBuffer, bufferLength, ref currentBufferPosition, false, out int token))
                    {
                        _logger.LogTrace("Matched tokens: {0}, position: {1}.", string.Join(" ", match.Tokens.Select(t => t.Value)), currentBufferPosition);
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

    }
}

