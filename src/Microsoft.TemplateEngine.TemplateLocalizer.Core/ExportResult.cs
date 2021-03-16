// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Core
{
    public sealed class ExportResult
    {
        public string? TemplateJsonPath { get; set; }

        public bool Succeeded => ErrorMessage == null;

        public string? ErrorMessage { get; set; }

        public Exception? InnerException { get; set; }
    }
}
