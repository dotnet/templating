// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Edge.Template
{
    public class InputParameter
    {
        public InputParameter(string name, string? value)
        {
            Name = name;
            Value = value;
        }

        public static implicit operator InputParameter(KeyValuePair<string, string?> pair) => new InputParameter(pair.Key, pair.Value);

        public string Name { get; private set; }

        public string? Value { get; private set; }
    }
}
