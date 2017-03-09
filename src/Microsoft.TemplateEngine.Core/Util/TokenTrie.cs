using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Matching;

namespace Microsoft.TemplateEngine.Core.Util
{
    public class TokenTrie : Trie<Token>, ITokenTrie
    {
        private List<byte[]> _tokens = new List<byte[]>();
        private List<int> _lengths = new List<int>();

        public int Count => _tokens.Count;

        public int MaxLength { get; private set; }

        public int MinLength { get; private set; } = int.MaxValue;

        public IReadOnlyList<int> TokenLength => _lengths;

        public IReadOnlyList<byte[]> Tokens => _tokens;

        public int AddToken(byte[] token)
        {
            int count = _tokens.Count;
            AddToken(token, count);
            return count;
        }

        public void AddToken(byte[] token, int index)
        {
            _tokens.Add(token);
            _lengths.Add(token.Length);
            Token t = new Token(token, index);
            AddPath(token, t);

            if (token.Length > MaxLength)
            {
                MaxLength = token.Length;
            }

            if (token.Length < MinLength)
            {
                MinLength = token.Length;
            }
        }

        public void Append(ITokenTrie trie)
        {
            foreach (byte[] token in trie.Tokens)
            {
                AddToken(token);
            }
        }

        public ITokenTrieEvaluator CreateEvaluator()
        {
            return new TokenTrieEvaluator(this); 
        }

        public bool GetOperation(byte[] buffer, int bufferLength, ref int currentBufferPosition, out int token)
        {
            int originalPosition = currentBufferPosition;
            TrieEvaluator<Token> evaluator = new TrieEvaluator<Token>(this);
            TrieEvaluationDriver<Token> driver = new TrieEvaluationDriver<Token>(evaluator);
            TerminalLocation<Token> location = driver.Evaluate(buffer, bufferLength, true, 0, ref currentBufferPosition);

            if (location != null && location.Location + location.Terminal.Start == originalPosition)
            {
                token = location.Terminal.Index;
                currentBufferPosition = location.Location + location.Terminal.End;
                return true;
            }

            currentBufferPosition = originalPosition;
            token = -1;
            return false;
        }
    }

    public class TokenTrieEvaluator : TrieEvaluator<Token>, ITokenTrieEvaluator
    {
        private int _currentSequenceNumber;

        public TokenTrieEvaluator(Trie<Token> trie)
            : base(trie)
        {
        }

        public int BytesToKeepInBuffer => _currentSequenceNumber - OldestRequiredSequenceNumber + 1;

        public bool Accept(byte data, ref int bufferPosition, out int token)
        {
            ++_currentSequenceNumber;
            if(Accept(data, ref _currentSequenceNumber, out TerminalLocation<Token> terminal))
            {
                token = terminal.Terminal.Index;
                bufferPosition += _currentSequenceNumber - terminal.Location - terminal.Terminal.End;
                return true;
            }

            token = -1;
            return false;
        }

        public bool TryFinalizeMatchesInProgress(ref int bufferPosition, out int token)
        {
            FinalizeMatchesInProgress(ref _currentSequenceNumber, out TerminalLocation<Token> terminal);

            if(terminal != null)
            {
                token = terminal.Terminal.Index;
                bufferPosition += _currentSequenceNumber - terminal.Location - terminal.Terminal.End;
                return true;
            }

            token = -1;
            return false;
        }
    }

    public class Token : TerminalBase
    {
        public Token(byte[] token, int index, int start = 0, int end = -1)
        {
            Value = token;
            Index = index;
            Start = start;
            End = end != -1 ? end : token.Length;
        }

        public byte[] Value { get; }

        public int Index { get; }
    }
}
