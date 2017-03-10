using Microsoft.TemplateEngine.Core.Matching;

namespace Microsoft.TemplateEngine.Core.Util
{
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