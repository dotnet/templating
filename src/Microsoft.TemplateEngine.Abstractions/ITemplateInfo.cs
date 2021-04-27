// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions
{
    /// <summary>
    /// Template information, used to be stored in the template cache.
    /// This information is common for all templates that can be managed by different <see cref="IGenerator"/>s.
    /// </summary>
    public interface ITemplateInfo
    {
        string? Author { get; }

        string? Description { get; }

        IReadOnlyList<string> Classifications { get; }

        string? DefaultName { get; }

        string Identity { get; }

        Guid GeneratorId { get; }

        string? GroupIdentity { get; }

        int Precedence { get; }

        string Name { get; }

        [Obsolete("Templates support multiple short names, use ShortNameList instead")]
        string ShortName { get; }

        /// <summary>
        /// Gets template tags.
        /// In Orchestrator.RunnableProjects (template.json) parameter symbol with choices are also represented as tags.
        /// Non choice parameter symbols are stored as <see cref="ICacheParameter"/>in <see cref="ITemplateInfo.CacheParameters"/> collection.
        /// </summary>
        IReadOnlyDictionary<string, ICacheTag> Tags { get; }

        /// <summary>
        /// Gets cached template parameter definition.
        /// In Orchestrator.RunnableProjects (template.json) parameter symbols are cached (all types except 'choice'). Choice parameters are stored as <see cref="ICacheTag"/>in <see cref="ITemplateInfo.Tags"/> collection.
        /// </summary>
        IReadOnlyDictionary<string, ICacheParameter> CacheParameters { get; }

        [Obsolete("use " + nameof(Tags) + " and " + nameof(CacheParameters) + " collections instead.")]
        IReadOnlyList<ITemplateParameter> Parameters { get; }

        string MountPointUri { get; }

        string ConfigPlace { get; }

        string? LocaleConfigPlace { get; }

        string? HostConfigPlace { get; }

        string? ThirdPartyNotices { get; }

        IReadOnlyDictionary<string, IBaselineInfo> BaselineInfo { get; }

        [Obsolete("This property is obsolete.")]
        bool HasScriptRunningPostActions { get; set; }

        /// <summary>
        /// Gets the list of short names defined for the template.
        /// </summary>
        IReadOnlyList<string> ShortNameList { get; }
    }
}
