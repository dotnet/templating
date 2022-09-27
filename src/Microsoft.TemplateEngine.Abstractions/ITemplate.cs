// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Mount;

namespace Microsoft.TemplateEngine.Abstractions
{
    /// <summary>
    /// Defines the template that can be run by <see cref="IGenerator"/>.
    /// </summary>
    public interface ITemplate : ITemplateInfo, IDisposable
    {
        /// <summary>
        /// Gets generator that runs the template.
        /// </summary>
        IGenerator Generator { get; }

        /// <summary>
        /// Gets configuration file system entry.
        /// </summary>
        IFileSystemInfo Configuration { get; }

        /// <summary>
        /// Gets localization file system entry.
        /// </summary>
        [Obsolete("The field is obsolete.")]
        IFileSystemInfo? LocaleConfiguration { get; }

        /// <summary>
        /// Gets directory with template source files.
        /// </summary>
        IDirectory? TemplateSourceRoot { get; }

        /// <summary>
        /// Indicates whether he template should be created in a subdirectory under the output directory.
        /// </summary>
        bool IsNameAgreementWithFolderPreferred { get; }

        /// <summary>
        /// Gets the list of localizations available for template.
        /// Only available when <see cref="IGenerator.GetTemplatesFromMountPoint(IMountPoint)"/> is run.
        /// When using <see cref="IGenerator.TryLoadTemplateFromTemplateInfo(IEngineEnvironmentSettings, ITemplateInfo, out ITemplate?, string?)"/> only the required localization is loaded.
        /// </summary>
        IReadOnlyDictionary<string, ILocalizationLocator>? Localizations { get; }

        /// <summary>
        /// Gets the list of validation errors discovered when loading the template.
        /// </summary>
        IReadOnlyList<IValidationEntry> ValidationErrors { get; }
    }
}
