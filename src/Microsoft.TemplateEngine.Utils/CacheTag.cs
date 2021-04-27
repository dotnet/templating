// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Utils
{
    /// <summary>
    /// Default <see cref="ICacheTag"/> implementation, represents data model for cached template tag.
    /// In Orchestrator.RunnableProjects (template.json) choice parameter symbols and tags are cached as tags. Non-choice parameters are cached as <see cref="CacheParameter"/>in <see cref="ITemplateInfo.CacheParameters"/> collection.
    /// </summary>
    public class CacheTag : ICacheTag
    {
        public CacheTag(string? displayName, string? description, IReadOnlyDictionary<string, ParameterChoice> choices, string? defaultValue)
            : this(displayName, description, choices, defaultValue, null)
        {
        }

        public CacheTag(string? displayName, string? description, IReadOnlyDictionary<string, ParameterChoice> choices, string? defaultValue, string? defaultIfOptionWithoutValue)
        {
            DisplayName = displayName;
            Description = description;
            Choices = choices.CloneIfDifferentComparer(StringComparer.OrdinalIgnoreCase);
            DefaultValue = defaultValue;
            DefaultIfOptionWithoutValue = defaultIfOptionWithoutValue;
        }

        public string? DisplayName { get; }

        public string? Description { get; }

        public IReadOnlyDictionary<string, ParameterChoice> Choices { get; }

        public string? DefaultValue { get; }

        public string? DefaultIfOptionWithoutValue { get; set; }
    }
}
