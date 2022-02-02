﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text;
using System.Text.RegularExpressions;

namespace Dotnet_new3.IntegrationTests
{
    internal static class VerifyScrubbers
    {
        /// <summary>
        /// Removes content after "Details: ".
        /// </summary>
        internal static void ScrubDetails(this StringBuilder output)
        {
            output.ScrubByRegex("(Details: )([^\\r\\n]*)", $"Details: %DETAILS%");
        }

        /// <summary>
        /// Removes table header delimiter.
        /// </summary>
        internal static void ScrubTableHeaderDelimiter(this StringBuilder output)
        {
            output.ScrubByRegex("---[- ]*", "%TABLE HEADER DELIMITER%");
        }

        /// <summary>
        /// Replaces content matching <paramref name="pattern"/> with <paramref name="replacement"/>.
        /// </summary>
        internal static void ScrubByRegex(this StringBuilder output, string pattern, string replacement)
        {
            string finalOutput = Regex.Replace(output.ToString(), pattern, replacement);
            output.Clear();
            output.Append(finalOutput);
        }
    }
}
