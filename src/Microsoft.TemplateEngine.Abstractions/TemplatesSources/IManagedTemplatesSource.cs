using Microsoft.TemplateEngine.Abstractions.Installer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Abstractions.TemplatesSources
{
    /// <summary>
    /// This is more advanced <see cref="ITemplatesSource"/>. Managed means that this templates source can be:<br/>
    /// Uninstalled via <see cref="ManagedProvider"/>.<see cref="IManagedTemplatesSourcesProvider.UninstallAsync(IManagedTemplatesSource)"/><br/>
    /// Check for latest version via <see cref="ManagedProvider"/>.<see cref="IManagedTemplatesSourcesProvider.GetLatestVersions(IEnumerable{IManagedTemplatesSource})"/><br/>
    /// Updated  via <see cref="ManagedProvider"/>.<see cref="IManagedTemplatesSourcesProvider.UpdateAsync(IEnumerable{IManagedTemplatesSourceUpdate})"/>
    /// </summary>
    public interface IManagedTemplatesSource : ITemplatesSource
    {
        /// <summary>
        /// This can be NuGet PackageId, path to .nupkg, folder name, or something similar that
        /// identifies this templates source to user.
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// Version of templates source.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// This is list of details that we show to user about this templates source like, author and other information
        /// that might be important to user.
        /// </summary>
        IReadOnlyDictionary<string, string> Details { get; }

        /// <summary>
        /// Allows ordering of <see cref="Details"/> when shown to user.
        /// </summary>
        IReadOnlyList<string> DetailKeysDisplayOrder { get; }

        /// <summary>
        /// Installer that created this source.
        /// This serves as helper for grouping sources by installer
        /// so caller doesn't need to keep track of installer->source relation.
        /// </summary>
        IInstaller Installer { get; }
    }
}
