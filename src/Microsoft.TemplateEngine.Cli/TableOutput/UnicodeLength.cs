// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.TemplateEngine.Cli.TableOutput
{
    internal static class UnicodeLength
    {
        internal static int GetUnicodeLength (this string s)
        {
            return s.Sum(ch => Wcwidth.UnicodeCalculator.GetWidth((int)ch));
        }
    }
}
