using System;

namespace Microsoft.TemplateEngine.Core.Expressions
{
    public class UnaryScope<TOperator> : IEvaluable
    {
        private readonly Func<object, object> _evaluate;

        public UnaryScope(IEvaluable parent, TOperator @operator, Func<object, object> evaluate)
        {
            Parent = parent;
            Operator = @operator;
            _evaluate = evaluate;
        }

        public IEvaluable Parent { get; set; }

        public TOperator Operator { get; }

        public IEvaluable Operand { get; set; }

        public object Evaluate()
        {
            object operand = Operand.Evaluate();
            return _evaluate(operand);
        }

        public bool IsIndivisible => true;

        public bool IsFull => Operand != null;

        public bool TryAccept(IEvaluable child)
        {
            if (Operand == null)
            {
                Operand = child;
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return $@"{Operator}({Operand})";
        }
    }
}