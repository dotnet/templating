using System;

namespace Microsoft.TemplateEngine.Core.Expressions
{
    public class BinaryScope<TOperator> : IEvaluable
    {
        private readonly Func<object, object, object> _evaluate;

        public BinaryScope(IEvaluable parent, TOperator @operator, Func<object, object, object> evaluate)
        {
            Parent = parent;
            Operator = @operator;
            _evaluate = evaluate;
        }

        public IEvaluable Parent { get; set; }

        public TOperator Operator { get; }

        public IEvaluable Left { get; set; }

        public IEvaluable Right { get; set; }

        public bool IsFull => Left != null && Right != null;

        public bool TryAccept(IEvaluable child)
        {
            if (Left == null)
            {
                Left = child;
                return true;
            }

            if (Right == null)
            {
                Right = child;
                return true;
            }

            return false;
        }

        public bool IsIndivisible => false;

        public object Evaluate()
        {
            object left = Left.Evaluate();
            object right = Right.Evaluate();
            return _evaluate(left, right);
        }

        public override string ToString()
        {
            return $@"({Left} -{Operator}- {Right})";
        }
    }
}