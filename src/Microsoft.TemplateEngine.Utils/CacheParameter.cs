// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Utils
{
    /// <summary>
    /// Default <see cref="ICacheParameter"/> implementation, represents data model for cached template parameter.
    /// In Orchestrator.RunnableProjects (template.json) parameter symbols are cached (all types except 'choice'). Choice parameters are stored as <see cref="CacheTag"/>in <see cref="ITemplateInfo.Tags"/> collection.
    /// </summary>
    public class CacheParameter : ICacheParameter
    {
        public CacheParameter(
            string? dataType = null,
            string? displayName = null,
            string? description = null,
            string? defaultValue = null,
            string? defaultIfOptionWithoutValue = null)
        {
            DataType = dataType;
            DisplayName = displayName;
            Description = description;
            DefaultValue = defaultValue;
            DefaultIfOptionWithoutValue = defaultIfOptionWithoutValue;
        }

        public string? DataType { get; }

        public string? DefaultValue { get; }

        public string? DisplayName { get; }

        public string? Description { get; }

        public string? DefaultIfOptionWithoutValue { get; }
    }
}
