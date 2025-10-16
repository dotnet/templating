// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Edge.Template
{
    [Obsolete("Use ITemplateMatchInfo instead")]
    public interface IFilteredTemplateInfo
    {
        ITemplateInfo Info { get; }

        IReadOnlyList<MatchInfo> MatchDisposition { get; }

        bool IsMatch { get; }

        bool IsPartialMatch { get; }

        bool HasParameterMismatch { get; }

        bool IsParameterMatch { get; }

        bool HasInvalidParameterValue { get; }

        bool HasAmbiguousParameterMatch { get; }
    }
}
