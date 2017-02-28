namespace UltraTrie
{
    public class TrieEvaluationDriver<T>
        where T : TerminalBase
    {
        private readonly TrieEvaluator<T> _evaluator;
        private int _sequenceNumber;

        public TrieEvaluationDriver(Trie<T> trie)
        {
            _evaluator = new TrieEvaluator<T>(trie);
        }

        public TerminalLocation<T> Evaluate(byte[] buffer, int bufferLength, bool isFinalBuffer, int lastNetBufferEffect, ref int bufferPosition)
        {
            TerminalLocation<T> terminal;
            _sequenceNumber += lastNetBufferEffect;
            int sequenceNumberToBufferPositionRelationship = _sequenceNumber - bufferPosition;

            if (lastNetBufferEffect != 0 || !_evaluator.TryGetNext(isFinalBuffer && bufferPosition >= bufferLength, ref _sequenceNumber, out terminal))
            {
                while (!_evaluator.Accept(buffer[bufferPosition], ref _sequenceNumber, out terminal))
                {
                    ++_sequenceNumber;
                    ++bufferPosition;

                    if (bufferPosition >= bufferLength)
                    {
                        if (!isFinalBuffer)
                        {
                            //TODO: Advance the buffer, preserving data from _evaluator.OldestRequiredSequenceNumber on
                            //  this is _sequenceNumber - _evaluator.OldestRequiredSequenceNumber bytes
                            //  bufferPosition should be reset to that value, but _sequenceNumber should remain unchanged
                            sequenceNumberToBufferPositionRelationship = _sequenceNumber - bufferPosition;
                        }
                        else
                        {
                            _evaluator.FinalizeMatchesInProgress(ref _sequenceNumber, out terminal);
                            break;
                        }
                    }
                }
            }

            if (terminal != null)
            {
                terminal.Location -= sequenceNumberToBufferPositionRelationship;
            }

            return terminal;
        }
    }
}