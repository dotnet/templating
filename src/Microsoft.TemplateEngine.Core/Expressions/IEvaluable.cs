namespace Microsoft.TemplateEngine.Core.Expressions
{
    public interface IEvaluable
    {
        IEvaluable Parent { get; set; }

        object Evaluate();

        bool IsIndivisible { get; }

        bool IsFull { get; }

        bool TryAccept(IEvaluable child);
    }
}