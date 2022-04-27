// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Utils
{
    public class MultiValue
    {
        public MultiValue(IReadOnlyList<string> values)
        {
            Values = values;
        }

        public IReadOnlyList<string> Values { get; private init; }

        public override string ToString() => string.Join("|", Values);
    }
}
