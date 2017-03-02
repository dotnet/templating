namespace Microsoft.TemplateEngine.Core.Matching
{
    public abstract class TerminalBase
    {
        public int Start { get; protected set; }

        public int End { get; protected set; }
    }
}