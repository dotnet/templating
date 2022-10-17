// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.TemplateEngine.Utils;

/// <summary>
/// Type to be used where we need all pattern matches to succeed.
/// </summary>
public class MatchAllGlob : IPatternMatcher
{
    /// <summary>
    /// Instance of Matcher satisfying all patterns tests.
    /// </summary>
    public static readonly IPatternMatcher Instance = new MatchAllGlob();

    private MatchAllGlob()
    { }

    /// <inheritdoc />
    public bool IsMatch(string test) => true;
}
