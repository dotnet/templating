// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Core
{
    public struct ExportOptions
    {
        /// <summary>
        /// Gets the default list of languages for which the localizable files will be exported.
        /// </summary>
        public static IReadOnlyList<string> DefaultLanguages { get; } = new[]
        {
            "cs",
            "de",
            "en",
            "es",
            "fr",
            "it",
            "ja",
            "ko",
            "pl",
            "pt-BR",
            "ru",
            "tr",
            "zh-Hans",
            "zh-Hant",
        };

        /// <summary>
        /// Gets or sets the languages for which localizable files will be exported.
        /// </summary>
        public IEnumerable<string>? Languages { get; set; }

        /// <summary>
        /// Full path of the directory to export into. If null, files will be exported into
        /// a "localize" folder next to the template.json file.
        /// </summary>
        public string? TargetDirectory { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether the export process should skip
        /// flushing the file changes to file system.
        /// </summary>
        public bool DryRun { get; set; }
    }
}
