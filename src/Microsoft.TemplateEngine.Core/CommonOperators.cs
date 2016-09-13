using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Core.Expressions;

namespace Microsoft.TemplateEngine.Core
{
    public enum Operators
    {
        And,
        Or,
        Xor,
        Not,
        GreaterThan,
        GreaterThanOrEqualTo,
        LessThan,
        LessThanOrEqualTo,
        EqualTo,
        NotEqualTo,
        Identity,
    }

    public static class CommonOperators
    {

        public static readonly IReadOnlyDictionary<Operators, Func<IEvaluable, IEvaluable>> OperatorScopeFactoryLookup = new Dictionary<Operators, Func<IEvaluable, IEvaluable>>
        {
            {Operators.And, CreateAnd},
            {Operators.Or, CreateOr},
            {Operators.Xor, CreateXor},
            {Operators.Not, x => CreateUnaryChild(x, Operators.Not, Not)},
            {Operators.GreaterThan, CreateGreaterThan},
            {Operators.GreaterThanOrEqualTo, CreateGreaterThanOrEqualTo},
            {Operators.LessThan, CreateLessThan},
            {Operators.LessThanOrEqualTo, CreateLessThanOrEqualTo},
            {Operators.EqualTo, CreateEquals},
            {Operators.NotEqualTo, CreateNotEquals},
        };

        private static object And(object left, object right)
        {
            bool l = (bool)Convert.ChangeType(left, typeof(bool));
            bool r = (bool)Convert.ChangeType(right, typeof(bool));

            return l && r;
        }

        private static IEvaluable CreateAnd(IEvaluable active)
        {
            return CreateBinaryChild(active, Operators.And, x => true, And);
        }

        private static IEvaluable CreateBinaryChild(IEvaluable active, Operators op, Func<Operators, bool> precedesOperator, Func<object, object, object> evaluate)
        {
            BinaryScope<Operators> self;

            //If we could steal an arg...
            if (!active.IsIndivisible)
            {
                BinaryScope<Operators> left = active as BinaryScope<Operators>;
                if (left != null && precedesOperator(left.Operator))
                {
                    self = new BinaryScope<Operators>(active, op, evaluate)
                    {
                        Left = left.Right
                    };
                    left.Right.Parent = self;
                    left.Right = self;
                    return self;
                }
            }

            //We couldn't steal an arg, "active" is now our left, inject ourselves into
            //  active's parent in its place
            self = new BinaryScope<Operators>(active.Parent, op, evaluate);

            if (active.Parent != null)
            {
                UnaryScope<Operators> unary = active.Parent as UnaryScope<Operators>;

                if (unary != null)
                {
                    unary.Parent = self;
                }
                else
                {
                    BinaryScope<Operators> binary = active.Parent as BinaryScope<Operators>;

                    if (binary != null)
                    {
                        if (binary.Left == active)
                        {
                            binary.Left = self;
                        }
                        else if (binary.Right == active)
                        {
                            binary.Right = self;
                        }
                    }
                }
            }

            active.Parent = self;
            self.Left = active;
            return self;
        }

        private static IEvaluable CreateEquals(IEvaluable active)
        {
            return CreateBinaryChild(active, Operators.EqualTo, x => false, Equals);
        }

        private static IEvaluable CreateGreaterThan(IEvaluable active)
        {
            return CreateBinaryChild(active, Operators.GreaterThan, x => false, (l, r) => ((IComparable)l).CompareTo(r) > 0);
        }

        private static IEvaluable CreateGreaterThanOrEqualTo(IEvaluable active)
        {
            return CreateBinaryChild(active, Operators.GreaterThanOrEqualTo, x => false, (l, r) => ((IComparable)l).CompareTo(r) >= 0);
        }

        private static IEvaluable CreateLessThan(IEvaluable active)
        {
            return CreateBinaryChild(active, Operators.LessThan, x => false, (l, r) => ((IComparable)l).CompareTo(r) > 0);
        }

        private static IEvaluable CreateLessThanOrEqualTo(IEvaluable active)
        {
            return CreateBinaryChild(active, Operators.LessThanOrEqualTo, x => false, (l, r) => ((IComparable)l).CompareTo(r) >= 0);
        }

        private static IEvaluable CreateNotEquals(IEvaluable active)
        {
            return CreateBinaryChild(active, Operators.NotEqualTo, x => false, NotEquals);
        }

        private static IEvaluable CreateOr(IEvaluable active)
        {
            return CreateBinaryChild(active, Operators.Or, x => x != Operators.Or, Or);
        }

        private static IEvaluable CreateUnaryChild(IEvaluable active, Operators op, Func<object, object> evaluate)
        {
            UnaryScope<Operators> self = new UnaryScope<Operators>(active, op, evaluate);
            active.TryAccept(self);
            return self;
        }

        private static IEvaluable CreateXor(IEvaluable active)
        {
            return CreateBinaryChild(active, Operators.Xor, x => x != Operators.And && x != Operators.Or, Xor);
        }

        private new static object Equals(object left, object right)
        {
            string l = left as string;
            string r = right as string;

            if (l != null && r != null)
            {
                return string.Equals(l, r, StringComparison.OrdinalIgnoreCase);
            }

            return object.Equals(l, r);
        }

        private static object Not(object operand)
        {
            bool l = (bool)Convert.ChangeType(operand, typeof(bool));

            return !l;
        }

        private static object NotEquals(object left, object right)
        {
            string l = left as string;
            string r = right as string;

            if (l != null && r != null)
            {
                return !string.Equals(l, r, StringComparison.OrdinalIgnoreCase);
            }

            return !object.Equals(l, r);
        }

        private static object Or(object left, object right)
        {
            bool l = (bool) Convert.ChangeType(left, typeof(bool));
            bool r = (bool) Convert.ChangeType(right, typeof(bool));

            return l || r;
        }

        private static object Xor(object left, object right)
        {
            bool l = (bool)Convert.ChangeType(left, typeof(bool));
            bool r = (bool)Convert.ChangeType(right, typeof(bool));

            return l ^ r;
        }
    }
}
