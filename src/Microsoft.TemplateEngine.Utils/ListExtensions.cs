﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.TemplateEngine.Utils
{
    public static class ListExtensions
    {
        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TElement, TKey>(this IEnumerable<TElement> elements, Func<TElement, TKey> grouper, Func<TElement, bool> hasGroupKey)
            where TKey : IEquatable<TKey>
        {
            Dictionary<ValueWrapper<TKey>, List<TElement>> groups = new Dictionary<ValueWrapper<TKey>, List<TElement>>();
            List<TElement> ungrouped = new List<TElement>();

            foreach (TElement element in elements)
            {
                if (hasGroupKey(element))
                {
                    ValueWrapper<TKey> x = new ValueWrapper<TKey>(grouper(element));
                    if (!groups.TryGetValue(x, out List<TElement> group))
                    {
                        groups[x] = group = new List<TElement>();
                    }

                    group.Add(element);
                }
                else
                {
                    ungrouped.Add(element);
                }
            }

            List<IGrouping<TKey, TElement>> allGrouped = new List<IGrouping<TKey, TElement>>();

            foreach (KeyValuePair<ValueWrapper<TKey>, List<TElement>> entry in groups)
            {
                allGrouped.Add(new Grouping<TKey, TElement>(entry.Key.Val, entry.Value));
            }

            foreach (TElement entry in ungrouped)
            {
                allGrouped.Add(new Grouping<TKey, TElement>(default(TKey), new[] { entry }));
            }

            return allGrouped;
        }

        private struct ValueWrapper<T>
        {
            public ValueWrapper(T val)
            {
                Val = val;
            }

            public override bool Equals(object obj)
            {
                return obj is ValueWrapper<T> v && Equals(Val, v.Val);
            }

            public override int GetHashCode()
            {
                return Val?.GetHashCode() ?? 0;
            }


            public T Val { get; private set; }
        }

        private class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
        {
            public Grouping(TKey key, IEnumerable<TElement> element)
            {
                Key = key;
                _element = element;
            }

            private IEnumerable<TElement> _element;

            public TKey Key { get; }

            public IEnumerator<TElement> GetEnumerator()
            {
                return _element.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
