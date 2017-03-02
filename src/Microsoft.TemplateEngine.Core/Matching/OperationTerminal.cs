using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Core.Matching
{
    public class OperationTerminal : TerminalBase
    {
        public IOperation Operation { get; }

        public int Token { get; }

        public OperationTerminal(IOperation operation, int token, int tokenLength, int start = 0, int end = -1)
        {
            Operation = operation;
            Token = token;
            Start = start;
            End = end != -1 ? end : tokenLength;
        }
    }
}
