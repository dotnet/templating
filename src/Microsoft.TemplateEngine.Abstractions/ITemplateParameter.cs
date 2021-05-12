// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions
{
    /// <summary>
    /// Template parameter definition used in <see cref="IParameterSet"/>.
    /// </summary>
    public interface ITemplateParameter
    {
        /// <summary>
        /// Gets documentation for template parameter.
        /// </summary>
        string? Documentation { get; }

        /// <summary>
        /// Gets parameter name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets parameter priority.
        /// </summary>
        TemplateParameterPriority Priority { get; }

        /// <summary>
        /// Gets parameter type.
        /// In Orchestrator.RunnableProjects the following types are used: parameter, generated, combined, derived, bind (same as symbol types).
        /// </summary>
        string? Type { get; }

        /// <summary>
        /// Returns true when parameter is default name symbol.
        /// </summary>
        bool IsName { get; }

        /// <summary>
        /// Gets the default value to be used if the parameter is not passed for template instantiation.
        /// </summary>
        string? DefaultValue { get; }

        /// <summary>
        /// Gets data type of parameter (boolean, string, choice, etc).
        /// </summary>
        string? DataType { get; }

        /// <summary>
        /// Gets collection of choices for choice <see cref="DataType"/>.
        /// <c>null</c> for other <see cref="DataType"/>s.
        /// </summary>
        IReadOnlyDictionary<string, ParameterChoice>? Choices { get; }

        /// <summary>
        /// Gets the default value to be used if the parameter is passed without value for template instantiation.
        /// </summary>
        string? DefaultIfOptionWithoutValue { get; }
    }
}
