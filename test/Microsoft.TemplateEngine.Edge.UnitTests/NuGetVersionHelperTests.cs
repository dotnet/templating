// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.TemplateEngine.Edge.Installers.NuGet;
using NuGet.Versioning;
using Xunit;

namespace Microsoft.TemplateEngine.Edge.UnitTests
{
    public class NuGetVersionHelperTests
    {
        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("*", true)]
        [InlineData("1.*", true)]
        [InlineData("55.66.77.*", true)]
        [InlineData("55.66.77*", true)]
        [InlineData("123.456.789.012", true)]
        [InlineData("1.2", true)]
        [InlineData("1.*.1", false)]
        [InlineData("1.*.*", false)]
        [InlineData("*.1", false)]
        [InlineData("a.b", false)]
        [InlineData("a.b.*", false)]
        public void IsSupportedVersionStringTest(string versionString, bool isSupported)
        {
            Assert.Equal(isSupported, NuGetVersionHelper.IsSupportedVersionString(versionString));
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("*", "")]
        [InlineData("1.*", "1.")]
        [InlineData("55.66.77.*", "55.66.77.")]
        [InlineData("55.66.77*", "55.66.77")]
        [InlineData("123.456.789.012", "123.456.789.012")]
        [InlineData("1.2", "1.2")]
        public void GetVersionPatternWithoutWildcardTest(string versionString, string patternWithoutWildcard)
        {
            Assert.Equal(patternWithoutWildcard, NuGetVersionHelper.GetVersionPatternWithoutWildcard(versionString));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("*", true)]
        [InlineData("1.*", true)]
        [InlineData("55.66.77.*", true)]
        [InlineData("55.66.77*", true)]
        [InlineData("123.456.789.012", false)]
        [InlineData("1.2", false)]
        public void IsFloatingVersionStringTest(string versionString, bool isFloatingVersion)
        {
            Assert.Equal(isFloatingVersion, NuGetVersionHelper.IsFloatingVersionString(versionString));
        }

        [Theory]
        [InlineData("1.2.3.4", null, true)]
        [InlineData("1.2.3.4", "", true)]
        [InlineData("1.2.3.4", "1", true)]
        [InlineData("1.2.3.4", "1.2.", true)]
        [InlineData("1.2.3.4", "1.2.3.4.", false)]
        [InlineData("1.2.3.4", "2.2", false)]
        public void VersionMatchesTest(string versionString, string patternWithoutWildcard, bool iaMatch)
        {
            NuGetVersion version = new NuGetVersion(versionString);
            Assert.Equal(iaMatch, NuGetVersionHelper.VersionMatches(version, patternWithoutWildcard));
        }
    }
}
