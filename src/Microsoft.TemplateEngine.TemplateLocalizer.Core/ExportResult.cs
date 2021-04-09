// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Core
{
    public sealed class ExportResult
    {
        public ExportResult(string? templateJsonPath, string? errorMessage, Exception? innerException)
        {
            TemplateJsonPath = templateJsonPath;
            ErrorMessage = errorMessage;
            InnerException = innerException;
        }

        public string? TemplateJsonPath { get; }

        public bool Succeeded => ErrorMessage == null && InnerException == null;

        public string? ErrorMessage { get; }

        public Exception? InnerException { get; }
    }
}
