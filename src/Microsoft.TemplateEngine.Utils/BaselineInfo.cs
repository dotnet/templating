﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Utils
{
    public class BaselineInfo : IBaselineInfo
    {
        public BaselineInfo(IReadOnlyDictionary<string, string> defaultOverrides, string? description = null)
        {
            DefaultOverrides = defaultOverrides;
            Description = description;
        }

        public string? Description { get; }

        public IReadOnlyDictionary<string, string> DefaultOverrides { get; }
    }
}
