// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Utils
{
    public static class TemplateParameterExtensions
    {
        private const char[]? _whitespaceSeparators = null;
        private static readonly char[] _multiValueSeparators = new[] { '|', ',' };

        public static bool IsChoice(this ITemplateParameter parameter)
        {
            return parameter.DataType?.Equals("choice", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public static IReadOnlyList<string> Tokenize(this string literal)
        {
            string[] tokens = literal.Split(_multiValueSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 1)
            {
                tokens = literal.Split(_whitespaceSeparators, StringSplitOptions.RemoveEmptyEntries);
            }

            return tokens.Select(t => t.Trim()).ToList();
        }
    }

}
