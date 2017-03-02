namespace Microsoft.TemplateEngine.Core.Matching
{
    public class TrieNode<T> : Trie<T>
        where T : TerminalBase
    {
        public readonly byte Match;

        public T Terminal;

        public TrieNode(byte match)
        {
            Match = match;
        }

        public bool IsTerminal => Terminal != null;
    }
}