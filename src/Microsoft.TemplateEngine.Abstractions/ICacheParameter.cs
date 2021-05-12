// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.TemplateEngine.Abstractions
{
    /// <summary>
    /// Defines the representation of a parameter in template cache.
    /// In Orchestrator.RunnableProjects (template.json) parameter symbols are cached (all types except 'choice'). Choice parameters are stored as <see cref="ICacheTag"/> in <see cref="ITemplateInfo.Tags"/> collection.
    /// </summary>
    public interface ICacheParameter
    {
        /// <summary>
        /// Gets the type of the parameter.
        /// </summary>
        string? DataType { get; }

        /// <summary>
        /// Gets the default value to be used for parameter if the parameter is not passed for template instantiation.
        /// </summary>
        string? DefaultValue { get; }

        /// <summary>
        /// Gets the friendly name of the parameter to be displayed to the user.
        /// This property is localized if localizations are provided.
        /// </summary>
        string? DisplayName { get; }

        /// <summary>
        /// Gets the detailed description of the parameter to be displayed to the user.
        /// This property is localized if localizations are provided.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Gets the default value to be used for parameter if the parameter is passed without value for template instantiation.
        /// </summary>
        string? DefaultIfOptionWithoutValue { get; }
    }
}
