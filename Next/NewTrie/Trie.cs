using System.Collections.Generic;

namespace UltraTrie
{
    public class Trie<T>
        where T : TerminalBase
    {
        public readonly Dictionary<byte, TrieNode<T>> NextNodes;

        public Trie()
        {
            NextNodes = new Dictionary<byte, TrieNode<T>>();
        }

        public int MaxRemainingLength { get; private set; }

        public void AddPath(byte[] path, T terminal)
        {
            if (path.Length > MaxRemainingLength)
            {
                MaxRemainingLength = path.Length;
            }

            int remainingLength = path.Length - 1;
            Dictionary<byte, TrieNode<T>> current = NextNodes;
            for (int i = 0; i < path.Length; ++i, --remainingLength)
            {
                TrieNode<T> next;
                if (!current.TryGetValue(path[i], out next))
                {
                    current[path[i]] = next = new TrieNode<T>(path[i])
                    {
                        MaxRemainingLength = remainingLength
                    };
                }
                else
                {
                    if (next.MaxRemainingLength < remainingLength)
                    {
                        next.MaxRemainingLength = remainingLength;
                    }
                }

                if (i == path.Length - 1)
                {
                    next.Terminal = terminal;
                }

                current = next.NextNodes;
            }
        }
    }
}