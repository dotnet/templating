using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Core.Matching
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
                    if(next.Terminals == null)
                    {
                        next.Terminals = new List<T>();
                    }

                    int sameMatcherIndex = next.Terminals.FindIndex(t => t.Start == terminal.Start && t.End == terminal.End);

                    if (sameMatcherIndex > -1)
                    {   // this matching is identical to another terminal already added to the trie. Overwrite it.
                        next.Terminals[sameMatcherIndex] = terminal;
                    }
                    else
                    {
                        next.Terminals.Add(terminal);
                    }
                }

                current = next.NextNodes;
            }
        }
    }
}
