// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.TemplateEngine.Abstractions
{
    /// <summary>
    /// Represents a validation error when loading the template.
    /// </summary>
    public interface IValidationEntry
    {
        /// <summary>
        /// Error severity.
        /// </summary>
        public enum SeverityLevel
        {
            None,
            Error,
            Warning,
            Info
        }

        /// <summary>
        /// Gets the error severity.
        /// </summary>
        SeverityLevel Severity { get; }

        /// <summary>
        /// Gets the code of the error.
        /// </summary>
        string Code { get; }

        /// <summary>
        /// Gets the error message (localized when available).
        /// </summary>
        string ErrorMessage { get; }

        /// <summary>
        /// Gets the filename where the error occurred.
        /// </summary>
        string? FileName { get; }
    }
}
