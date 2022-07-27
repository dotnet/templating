// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ConfigModel;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ConfigModel
{
    /// <summary>
    /// Defines the symbol of type "parameter".
    /// </summary>
    public sealed class ParameterSymbol : BaseValueSymbol
    {
        internal const string TypeName = "parameter";

        private IReadOnlyDictionary<string, ParameterChoice>? _choices;

        /// <summary>
        /// Creates a default instance of <see cref="ParameterSymbol"/>.
        /// </summary>
        internal ParameterSymbol(string name, string? replaces = null) : base(name, replaces) { }

        /// <summary>
        /// Creates an instance of <see cref="ParameterSymbol"/> using
        /// the provided JSON Data.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="jObject">JSON to initialize the symbol with.</param>
        /// <param name="defaultOverride"></param>
        internal ParameterSymbol(string name, JObject jObject, string? defaultOverride)
            : base(name, jObject, defaultOverride)
        {
            DefaultIfOptionWithoutValue = jObject.ToString(nameof(DefaultIfOptionWithoutValue));
            DisplayName = jObject.ToString(nameof(DisplayName)) ?? string.Empty;
            Description = jObject.ToString(nameof(Description)) ?? string.Empty;

            var choicesAndDescriptions = new Dictionary<string, ParameterChoice>();

            if (DataType == "choice")
            {
                IsTag = false;
                TagName = jObject.ToString(nameof(TagName));

                foreach (JObject choiceObject in jObject.Items<JObject>(nameof(Choices)))
                {
                    string? choiceName = choiceObject.ToString("choice");

                    if (string.IsNullOrWhiteSpace(choiceName))
                    {
                        throw new TemplateAuthoringException(string.Format(LocalizableStrings.SymbolModel_Error_MandatoryPropertyMissing, name, ParameterSymbol.TypeName, "choice"), name);
                    }

                    var choice = new ParameterChoice(
                        choiceObject.ToString("displayName") ?? string.Empty,
                        choiceObject.ToString("description") ?? string.Empty);

                    choicesAndDescriptions.Add(choiceName!, choice);
                }
            }
            else if (DataType == "bool" && string.IsNullOrEmpty(DefaultIfOptionWithoutValue))
            {
                // bool flags are considered true if they're provided without a value.
                DefaultIfOptionWithoutValue = "true";
            }

            Choices = choicesAndDescriptions;
            AllowMultipleValues = jObject.ToBool(nameof(AllowMultipleValues));
            EnableQuotelessLiterals = jObject.ToBool(nameof(EnableQuotelessLiterals));
        }

        /// <summary>
        /// Creates a clone of the given <see cref="ParameterSymbol"/>.
        /// </summary>
        /// <param name="cloneFrom">The symbol to copy the values from.</param>
        /// <param name="formsFallback">The value to be used for <see cref="BaseValueSymbol.Forms"/> in the case
        /// that the <paramref name="cloneFrom"/> does not specify a value for <see cref="BaseValueSymbol.Forms"/>.</param>
        internal ParameterSymbol(ParameterSymbol cloneFrom, SymbolValueFormsModel formsFallback) : base (cloneFrom, formsFallback)
        {
            Description = cloneFrom.Description;
            IsTag = cloneFrom.IsTag;
            TagName = cloneFrom.TagName;
            Choices = cloneFrom.Choices;
            AllowMultipleValues = cloneFrom.AllowMultipleValues;
            EnableQuotelessLiterals = cloneFrom.EnableQuotelessLiterals;
        }

        /// <inheritdoc/>
        public override string Type => TypeName;

        /// <summary>
        /// Gets the friendly name of the symbol to be displayed to the user.
        /// </summary>
        public string? DisplayName { get; private set; }

        /// <summary>
        /// Gets the description of the parameter.
        /// </summary>
        public string? Description { get; internal init; }

        /// <summary>
        /// If this is set, the option can be provided without a value. It will be given this value.
        /// </summary>
        public string? DefaultIfOptionWithoutValue { get; internal init; }

        /// <summary>
        /// If this is set, it's allowed to specify multiple values of that parameter.
        /// </summary>
        public bool AllowMultipleValues { get; internal init; }

        /// <summary>
        ///  If this is set, it's allowed to specify choice literals without quotation within conditions.
        /// </summary>
        public bool EnableQuotelessLiterals { get; internal init; }

        /// <summary>
        /// Defines choices for parameter of data type "choice".
        /// </summary>
        public IReadOnlyDictionary<string, ParameterChoice>? Choices
        {
            get
            {
                return _choices;
            }

            internal init
            {
                _choices = value?.CloneIfDifferentComparer(StringComparer.OrdinalIgnoreCase);
            }
        }

        // only relevant for choice datatype
        internal bool IsTag { get; init; }

        // only relevant for choice datatype
        internal string? TagName { get; init; }

        internal static ParameterSymbol FromDeprecatedConfigTag(string name, string value)
        {
            ParameterSymbol symbol = new ParameterSymbol(name)
            {
                DefaultValue = value,
                DataType = "choice",
                IsTag = true,
                Choices = new Dictionary<string, ParameterChoice>()
                {
                    { value, new ParameterChoice(string.Empty, string.Empty) }
                },
                Forms = SymbolValueFormsModel.Default
            };

            return symbol;
        }
    }
}
