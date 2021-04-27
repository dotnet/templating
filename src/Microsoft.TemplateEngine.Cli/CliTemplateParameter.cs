// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Cli
{
    internal class CliTemplateParameter
    {
        private IReadOnlyDictionary<string, ParameterChoice>? _choices;

        internal CliTemplateParameter(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
            }
            Name = name;
        }

        internal string? Documentation { get; set; }

        internal string Name { get; set; }

        internal TemplateParameterPriority Priority { get; set; }

        internal string? DefaultValue { get; set; }

        internal string? DataType { get; set; }

        internal string? DefaultIfOptionWithoutValue { get; set; }

        internal IReadOnlyDictionary<string, ParameterChoice>? Choices
        {
            get
            {
                return _choices;
            }
            set
            {
                _choices = value.CloneIfDifferentComparer(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
