// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TemplateEngine.Edge
{
    internal static class EnumerableExtensions
    {
        public static bool HasDuplicities<T>(this IEnumerable<T>? sequence, IEqualityComparer<T>? comparer = null)
        {
            if (sequence == null)
            {
                return false;
            }

            return sequence.GroupBy(x => x, comparer).Any(g => g.Count() > 1);
        }

        public static IEnumerable<T> GetDuplicities<T>(this IEnumerable<T>? sequence, IEqualityComparer<T>? comparer = null)
        {
            if (sequence == null)
            {
                return Enumerable.Empty<T>();
            }

            return sequence.GroupBy(x => x, comparer)
                .Where(g => g.Count() > 1)
                .Select(y => y.Key);
        }

        public static string ToCsvString<T>(this IEnumerable<T>? source, bool useSpace = true)
        {
            return source == null ? "<NULL>" : string.Join("," + (useSpace ? " " : string.Empty), source);
        }
    }
}
