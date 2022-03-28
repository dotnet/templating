// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGet.Versioning;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal static class NuGetVersionHelper
    {
        public static string GetVersionPatternWithoutWildcard(string? versionString)
        {
            if (IsSpecificVersionString(versionString) || string.IsNullOrEmpty(versionString))
            {
                return versionString ?? string.Empty;
            }

            return versionString!.Substring(0, versionString.Length - 1);
        }

        public static bool IsSupportedVersionString(string? versionString)
        {
            return
                IsFloatingVersionString(versionString) && IsSupportedFloatingVersion(versionString)
                ||
                NuGetVersion.TryParse(versionString, out _);
        }

        public static bool IsSpecificVersionString(string? versionString)
        {
            return !IsFloatingVersionString(versionString);
        }

        public static bool IsFloatingVersionString(string? versionString)
        {
            return
                string.IsNullOrEmpty(versionString) ||
                versionString.Last() == '*' && versionString.Count(c => c == '*') == 1;
        }

        public static bool VersionMatches(NuGetVersion version, string? versionPatternWithoutWildcard)
        {
            return
                string.IsNullOrEmpty(versionPatternWithoutWildcard) ||
                version.ToString().StartsWith(versionPatternWithoutWildcard, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSupportedFloatingVersion(string? versionString)
        {
            if (string.IsNullOrEmpty(versionString))
            {
                return true;
            }

            int trailingCharsToRemove = 1;
            if (versionString!.Length > 1 && versionString[versionString.Length - 2] == '.')
            {
                trailingCharsToRemove++;
            }

            string parseableVersionString = versionString.Substring(0, versionString.Length - trailingCharsToRemove);

            return
                string.IsNullOrEmpty(parseableVersionString) ||
                NuGetVersion.TryParse(parseableVersionString, out _);
        }
    }
}
