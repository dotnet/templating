// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.TemplateEngine.Abstractions
{
    /// <summary>
    /// Defines the priority of a template parameter.
    /// </summary>
    public enum TemplateParameterPriority
    {
        /// <summary>
        /// The parameter is mandatory.
        /// </summary>
        Required,

        [Obsolete("the value was never used and is deprecated.")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Suggested,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// The parameter is optional.
        /// </summary>
        Optional,

        /// <summary>
        /// The parameter is implicit (built-in).
        /// </summary>
        Implicit
    }
}
