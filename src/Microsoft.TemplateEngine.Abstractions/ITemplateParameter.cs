// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;

#pragma warning disable RS0016 // Add public types and members to the declared API
#pragma warning disable SA1507 // Code should not contain multiple blank lines in a row
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1516 // Elements should be separated by blank line

namespace Microsoft.TemplateEngine.Abstractions
{
    /// <summary>
    /// Template parameter definition.
    /// </summary>
    public interface ITemplateParameter : IEquatable<ITemplateParameter>
    {
        [Obsolete("Use Description instead.")]
        string? Documentation { get; }

        /// <summary>
        /// Gets parameter description.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Gets parameter name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets parameter priority.
        /// </summary>
        //TODO: [Obsolete("Use Precedence instead.")]
        TemplateParameterPriority Priority { get; }

        TemplateParameterPrecedence Precedence { get; }

        /// <summary>
        /// Gets parameter type.
        /// In Orchestrator.RunnableProjects the following types are used: parameter, generated, combined, derived, bind (same as symbol types).
        /// </summary>
        string Type { get; }

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
        string DataType { get; }

        /// <summary>
        /// Gets collection of choices for choice <see cref="DataType"/>.
        /// <c>null</c> for other <see cref="DataType"/>s.
        /// </summary>
        IReadOnlyDictionary<string, ParameterChoice>? Choices { get; }

        /// <summary>
        /// Gets the friendly name of the parameter to be displayed to the user.
        /// This property is localized if localizations are provided.
        /// </summary>
        string? DisplayName { get; }

        /// <summary>
        /// Indicates whether parameter arity is allowed to be > 1.
        /// </summary>
        bool AllowMultipleValues { get; }

        /// <summary>
        /// Gets the default value to be used if the parameter is passed without value for template instantiation.
        /// </summary>
        string? DefaultIfOptionWithoutValue { get; }
    }

    public class TemplateParameterPrecedence
    {
        public static readonly TemplateParameterPrecedence Default =
            new TemplateParameterPrecedence(PrecedenceDefinition.Optional, null, null);

        public TemplateParameterPrecedence(PrecedenceDefinition precedenceDefinition, string? isRequiredCondition, string? isEnabledCondition)
        {
            PrecedenceDefinition = precedenceDefinition;
            IsRequiredCondition = isRequiredCondition;
            IsEnabledCondition = isEnabledCondition;
            VerifyConditions();
        }

        public PrecedenceDefinition PrecedenceDefinition { get; }

        public string? IsRequiredCondition { get; }

        public string? IsEnabledCondition { get; }

        private void VerifyConditions()
        {
            // If enable condition is set - parameter is conditionally disabled (regardless if require condition is set or not)
            // Conditionally required is if and only if the only require condition is set

            if (!(string.IsNullOrEmpty(IsRequiredCondition) ^ PrecedenceDefinition == PrecedenceDefinition.ConditionalyRequired
                ||
                !string.IsNullOrEmpty(IsEnabledCondition) ^ PrecedenceDefinition == PrecedenceDefinition.ConditionalyDisabled)
                &&
                !(!string.IsNullOrEmpty(IsRequiredCondition) && !string.IsNullOrEmpty(IsEnabledCondition) && PrecedenceDefinition == PrecedenceDefinition.ConditionalyDisabled))
            {
                // TODO: localize
                throw new ArgumentException("Mismatched precedence definition");
            }
        }
    }

#pragma warning disable SA1204 // Static elements should appear before instance elements
    public static class TemplateParameterPrecedenceExtensions
#pragma warning restore SA1204 // Static elements should appear before instance elements
    {
        public static PrecedenceDefinition ToPrecedenceDefinition(this TemplateParameterPriority priority)
        {
            switch (priority)
            {
                case TemplateParameterPriority.Required:
                    return PrecedenceDefinition.Required;
                case TemplateParameterPriority.Optional:
                    return PrecedenceDefinition.Optional;
                case TemplateParameterPriority.Implicit:
                    return PrecedenceDefinition.Implicit;
                default:
                    throw new ArgumentOutOfRangeException(nameof(priority), priority, null);
            }
        }

        public static TemplateParameterPrecedence ToTemplateParameterPrecedence(this TemplateParameterPriority priority)
        {
            return new TemplateParameterPrecedence(priority.ToPrecedenceDefinition(), null, null);
        }
    }

    public enum PrecedenceDefinition
    {
        Required,
        ConditionalyRequired,
        Optional,
        Implicit,
        ConditionalyDisabled,
        Disabled,
    }

    public enum EvaluatedPrecedence
    {
        Required,
        Optional,
        Implicit,
        Disabled,
    }

#pragma warning restore RS0016 // Add public types and members to the declared API
#pragma warning restore SA1507 // Code should not contain multiple blank lines in a row
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1201 // Elements should appear in the correct order
#pragma warning restore SA1516 // Elements should be separated by blank line
}
