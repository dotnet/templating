// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;

namespace Microsoft.TemplateEngine.Edge.Template
{
    /// <summary>
    /// Represents match status.
    /// </summary>
    public enum MatchKind
    {
        [Obsolete("The value is deprecated")]
        Unspecified,

        /// <summary>
        /// exact full match: filter criteria is equal to template value.
        /// </summary>
        Exact,

        /// <summary>
        /// partial match:  template value contains filter criteria
        /// applicable only for <see cref="WellKnownSearchFilters.NameFilter(string)"/> and <see cref="WellKnownSearchFilters.ClassificationsFilter(string)"/>.
        /// </summary>
        Partial,

        /// <summary>
        /// mismatch: template value is different from filter criteria.
        /// </summary>
        Mismatch,

        /// <summary>
        /// the template does not have parameter with given name.
        /// applicable only for template symbols matching.
        /// </summary>
        InvalidParameterName,

        InvalidParameterValue,

        /// <summary>
        /// the template has multiple choice value which starts with filter criteria
        /// applicable only for template parameter matching, choice type
        /// </summary>
        [Obsolete("Will be deprecated in next release", false)]
        AmbiguousParameterValue,

        /// <summary>
        /// the template has single choice value which starts with filter criteria
        /// applicable only for template parameter matching, choice type
        /// </summary>
        [Obsolete("Will be deprecated in next release", false)]
        SingleStartsWith
    }
}
