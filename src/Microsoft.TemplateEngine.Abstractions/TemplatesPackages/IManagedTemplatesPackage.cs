// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Installer;

namespace Microsoft.TemplateEngine.Abstractions.TemplatesPackages
{
    /// <summary>
    /// This is more advanced <see cref="ITemplatesPackage"/>. Managed means that this templates source can be:<br/>
    /// Uninstalled via <see cref="ManagedProvider"/>.<see cref="IManagedTemplatesPackagesProvider.UninstallAsync(IManagedTemplatesPackage)"/><br/>
    /// Check for latest version via <see cref="ManagedProvider"/>.<see cref="IManagedTemplatesPackagesProvider.GetLatestVersionsAsync(IEnumerable{IManagedTemplatesPackage}, System.Threading.CancellationToken)"/><br/>
    /// Updated  via <see cref="ManagedProvider"/>.<see cref="IManagedTemplatesPackagesProvider.UpdateAsync(IEnumerable{ManagedTemplatesPackageUpdate})"/>
    /// </summary>
    public interface IManagedTemplatesPackage : ITemplatesPackage
    {
        /// <summary>
        /// The name to be used when displaying source in UI.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// This can be NuGet PackageId, path to .nupkg, folder name, or something similar that
        /// identifies this templates source to user.
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// Installer that created this source.
        /// This serves as helper for grouping sources by installer
        /// so caller doesn't need to keep track of installer->source relation.
        /// </summary>
        IInstaller Installer { get; }

        /// <summary>
        /// Installer that created this source.
        /// This serves as helper for grouping sources by <see cref="IManagedTemplatesPackagesProvider"/>
        /// so caller doesn't need to keep track of "managed provider"->"source" relation.
        /// </summary>
        IManagedTemplatesPackagesProvider ManagedProvider { get; }

        /// <summary>
        /// Version of templates source.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// This is list of details that we show to user about this templates source like author and other information
        /// that might be important to user.
        /// </summary>
        IReadOnlyDictionary<string, string> GetDisplayDetails();
    }
}
