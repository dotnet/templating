using System;
using System.Collections.Generic;

namespace JsonModelGenerator
{
    internal class CompareUsings : IComparer<string>
    {
        public static IComparer<string> Default { get; } = new CompareUsings();

        public int Compare(string x, string y)
        {
            x = x.Replace(';', '.');
            y = y.Replace(';', '.');

            bool systemX = x.Contains(" System.");
            bool systemY = y.Contains(" System.");

            if (systemX == systemY)
            {
                return StringComparer.Ordinal.Compare(x, y);
            }

            return systemX ? -1 : string.Equals(x, y, StringComparison.Ordinal) ? 0 : 1;
        }
    }
}
