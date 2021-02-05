// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.TemplateEngine.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.TemplateEngine.Utils
{
    public class InstallRequestPathResolution
    {
        private static IReadOnlyList<string> DetermineDirectoriesToScan(string baseDir, IEngineEnvironmentSettings environmentSettings)
        {
            List<string> directoriesToScan = new List<string>();

            if (baseDir[baseDir.Length - 1] == '/' || baseDir[baseDir.Length - 1] == '\\')
            {
                baseDir = baseDir.Substring(0, baseDir.Length - 1);
            }

            string searchTarget = Path.Combine(environmentSettings.Host.FileSystem.GetCurrentDirectory(), baseDir.Trim());
            List<string> matches = environmentSettings.Host.FileSystem.EnumerateFileSystemEntries(Path.GetDirectoryName(searchTarget), Path.GetFileName(searchTarget), SearchOption.TopDirectoryOnly).ToList();

            if (matches.Count == 1)
            {
                directoriesToScan.Add(matches[0]);
            }
            else
            {
                foreach (string match in matches)
                {
                    IReadOnlyList<string> subDirMatches = DetermineDirectoriesToScan(match, environmentSettings);
                    directoriesToScan.AddRange(subDirMatches);
                }
            }

            return directoriesToScan;
        }

        public static IEnumerable<string> Expand(string unexpandedInstallRequest, IEngineEnvironmentSettings environmentSettings)
        {
            // Handle cases like PackageId::Version
            if (unexpandedInstallRequest.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                yield return unexpandedInstallRequest;
                yield break;
            }
            var matches = DetermineDirectoriesToScan(unexpandedInstallRequest, environmentSettings).ToList();
            //This can happen when user specifies "PackageId"
            if (matches.Count == 0)
            {
                yield return unexpandedInstallRequest;
                yield break;
            }
            foreach (var path in matches)
            {
                yield return Path.GetFullPath(path);
            }
        }
    }
}
