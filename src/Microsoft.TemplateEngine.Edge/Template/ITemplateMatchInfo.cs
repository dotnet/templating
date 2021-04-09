// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Edge.Template
{
    /// <summary>
    /// Template with information about matching the filters.
    /// </summary>
    public interface ITemplateMatchInfo
    {
        /// <summary>
        /// True when the template matched all the filters applied.
        /// </summary>
        bool IsMatch { get; }

        /// <summary>
        /// True when the template matched at least one of the filters applied.
        /// </summary>
        bool IsPartialMatch { get; }

        /// <summary>
        /// Gets the template the filters applied to.
        /// </summary>
        ITemplateInfo Info { get; }

        /// <summary>
        /// Gets match information.
        /// </summary>
        IReadOnlyList<MatchInfo> MatchDisposition { get; }

        [Obsolete("Use MatchDisposition instead")]
        IReadOnlyList<MatchInfo> DispositionOfDefaults { get; }

        /// <summary>
        /// Adds the match information.
        /// </summary>
        /// <param name="newDisposition"></param>
        void AddDisposition(MatchInfo newDisposition);
    }
}
