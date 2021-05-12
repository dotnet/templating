// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    internal class Parameter : ITemplateParameter
    {
        internal Parameter(
            string name,
            string? type = null,
            string? dataType = null,
            bool isName = false,
            TemplateParameterPriority priority = default,
            string? documentation = null,
            string? defaultValue = null,
            string? defaultIfOptionWithoutValue = null,
            IReadOnlyDictionary<string, ParameterChoice>? choices = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
            }

            Name = name;
            Type = type;
            DataType = dataType;
            Priority = priority;
            Documentation = documentation;
            DefaultValue = defaultValue;
            DefaultIfOptionWithoutValue = defaultIfOptionWithoutValue;
            Choices = choices ?? new Dictionary<string, ParameterChoice>();
            IsName = isName;
        }

        public IReadOnlyDictionary<string, ParameterChoice> Choices { get; }

        public string? Documentation { get; }

        public TemplateParameterPriority Priority { get; }

        public string? DefaultValue { get; }

        public string Name { get; internal set; }

        public bool IsName { get; }

        public string? Type { get; }

        public string? DataType { get; internal set; }

        public string? DefaultIfOptionWithoutValue { get; }

        public override string ToString()
        {
            return $"{Name} ({Type})";
        }
    }
}
