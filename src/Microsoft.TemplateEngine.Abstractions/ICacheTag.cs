// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions
{
    /// <summary>
    /// Represents a template tag in template cache file.
    /// In Orchestrator.RunnableProjects (template.json) parameter symbol with choices are also represented as tags.
    /// Non choice parameter symbols are stored as <see cref="ICacheParameter"/>in <see cref="ITemplateInfo.CacheParameters"/> collection.
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

        /// <summary>
        /// Gets the dictionary containing the possible choices for this tag or parameter symbol.
        /// Keys represents the identifiers of the choices.
        /// </summary>
        IReadOnlyDictionary<string, ParameterChoice> Choices { get; }

        /// <summary>
        /// Gets the identifier of the default choice to be used when no choice was explicitly selected.
        /// </summary>
        string? DefaultValue { get; }

        /// <summary>
        /// Gets the default value to be used if the user specified the option without value.
        /// </summary>
        string? DefaultIfOptionWithoutValue { get; }
    }
}
