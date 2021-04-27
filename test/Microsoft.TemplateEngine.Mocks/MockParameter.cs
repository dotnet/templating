// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Mocks
{
    public class MockParameter : ITemplateParameter
    {
        public MockParameter(string name)
        {
            Name = name;
        }
        public string? Documentation { get; set; }

        public string Name { get; set; }

        public TemplateParameterPriority Priority { get; set; }

        public string? Type { get; set; }

        public bool IsName { get; set; }

        public string? DefaultValue { get; set; }

        public string? DefaultIfOptionWithoutValue { get; set; }

        public string? DataType { get; set; }

        public IReadOnlyDictionary<string, ParameterChoice>? Choices { get; set; }
    }
}
