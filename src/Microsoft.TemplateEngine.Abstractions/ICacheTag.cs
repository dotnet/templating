// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions
{
    /// <summary>
    /// Represents a tag or a parameter symbol with choices in template cache file.
    /// </summary>
    public interface ICacheTag
    {
        /// <summary>
        /// Gets the friendly name of the choice parameter symbol to be displayed to the user.
        /// </summary>
        string? DisplayName { get; }

        /// <summary>
        /// Gets the description of the choice parameter symbol to be displayed to the user.
        /// </summary>
        string? Description { get; }

        IReadOnlyDictionary<string, ParameterChoice> Choices { get; }

        string? DefaultValue { get; }
    }
}
