using System;
using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Utils
{
    public static class EqualityExtensions
    {
        public static bool AllAreTheSame<T, TValue>(this IEnumerable<T> items, Func<T, TValue> selector, bool noElementsValue = true)
            where TValue : IEquatable<TValue>
        {
            return items.AllAreTheSame(selector, (x, y) => x?.Equals(y) ?? y == null, noElementsValue);
        }

        public static bool AllAreTheSame<T, TValue>(this IEnumerable<T> items, Func<T, TValue> selector, IEqualityComparer<TValue> comparer, bool noElementsValue = true)
            where TValue : IEquatable<TValue>
        {
            if (comparer == null)
            {
                comparer = EqualityComparer<TValue>.Default;
            }

            return items.AllAreTheSame(selector, comparer.Equals, noElementsValue);
        }

        public static bool AllAreTheSame<T, TValue>(this IEnumerable<T> items, Func<T, TValue> selector, Func<TValue, TValue, bool> comparer, bool noElementsValue = true)
            where TValue : IEquatable<TValue>
        {
            if (comparer == null)
            {
                comparer = EqualityComparer<TValue>.Default.Equals;
            }

            using (IEnumerator<T> enumerator = items.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    return noElementsValue;
                }

                TValue firstValue = selector(enumerator.Current);

                while (enumerator.MoveNext())
                {
                    TValue currentValue = selector(enumerator.Current);

                    if (!comparer(firstValue, currentValue))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
