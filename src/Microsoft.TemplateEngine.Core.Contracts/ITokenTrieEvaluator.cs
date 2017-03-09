namespace Microsoft.TemplateEngine.Core.Contracts
{
    public interface ITokenTrieEvaluator
    {
        bool Accept(byte data, ref int bufferPosition, out int token);

        bool TryFinalizeMatchesInProgress(ref int bufferPosition, out int token);
    }
}