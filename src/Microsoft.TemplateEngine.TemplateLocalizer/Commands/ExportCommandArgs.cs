// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Commands
{
    /// <summary>
    /// Model class representing the arguments of <see cref="ExportCommand"/>.
    /// </summary>
    internal sealed class ExportCommandArgs
    {
        public ExportCommandArgs(IEnumerable<TemplateJsonProvider> templatePath, IEnumerable<string> language, bool recursive)
        {
            TemplateJsonProviders = templatePath;
            Languages = language;
            SearchSubdirectories = recursive;
        }

        /// <summary>
        /// Gets or sets the template.json file providers.
        /// </summary>
        public IEnumerable<TemplateJsonProvider> TemplateJsonProviders { get; set; }

        /// <summary>
        /// Gets or sets the languages for which the localization files should be created.
        /// </summary>
        public IEnumerable<string> Languages { get; set; }

        /// <summary>
        /// Gets or sets if subdirectories should be searched by <see cref="TemplateJsonProviders"/>.
        /// </summary>
        public bool SearchSubdirectories { get; set; }
    }
}
