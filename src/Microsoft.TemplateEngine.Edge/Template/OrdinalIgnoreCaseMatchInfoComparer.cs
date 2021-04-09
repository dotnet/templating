// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Edge.Template
{
    public class OrdinalIgnoreCaseMatchInfoComparer : IEqualityComparer<MatchInfo>
    {
        public bool Equals(MatchInfo x, MatchInfo y)
        {
            return x.Kind == y.Kind
                && string.Equals(x.ParameterName, y.ParameterName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(x.ParameterValue, y.ParameterValue, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(MatchInfo obj)
        {
            return new { a = obj.ParameterName?.ToLowerInvariant(), b = obj.ParameterValue?.ToLowerInvariant(), obj.Kind, }.GetHashCode();
        }
    }
}
