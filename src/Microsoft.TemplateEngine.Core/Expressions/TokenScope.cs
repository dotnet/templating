namespace Microsoft.TemplateEngine.Core.Expressions
{
    public class TokenScope<TToken> : IEvaluable
    {
        public TokenScope(IEvaluable parent, Token<TToken> token)
        {
            Parent = parent;
            Token = token;
        }

        public IEvaluable Parent { get; set; }

        public Token<TToken> Token { get; }

        public object Evaluate()
        {
            return Token.Value;
        }

        public bool IsIndivisible => true;

        public bool IsFull => true;

        public bool TryAccept(IEvaluable child) => false;

        public override string ToString()
        {
            return $@"""{Token.Value}""";
        }
    }
}