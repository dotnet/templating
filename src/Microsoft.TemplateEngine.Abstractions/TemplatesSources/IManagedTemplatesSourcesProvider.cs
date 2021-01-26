using Microsoft.TemplateEngine.Abstractions.Installer;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Abstractions.TemplatesSources
{
    /// <summary>
    /// This provider is responsible for managing <see cref="IManagedTemplatesSource"/>.
    /// </summary>
    public interface IManagedTemplatesSourcesProvider : ITemplatesSourcesProvider
    {
        /// <summary>
        /// Takes list of <see cref="IManagedTemplatesSource"/> as input so it can check for latest versions in batch.
        /// And returns list of <see cref="IManagedTemplatesSourceUpdate"/> which contains original <see cref="IManagedTemplatesSource"/>
        /// so caller can compare <see cref="IManagedTemplatesSourceUpdate.Version"/> with <see cref="IManagedTemplatesSource.Version"/>
        /// </summary>
        /// <param name="sources">List of <see cref="IManagedTemplatesSource"/> to get latest version for.</param>
        /// <returns>List of <see cref="IManagedTemplatesSourceUpdate"/></returns>
        Task<IReadOnlyList<IManagedTemplatesSourceUpdate>> GetLatestVersions(IEnumerable<IManagedTemplatesSource> sources);

        /// <summary>
        /// Updates specified <see cref="IManagedTemplatesSource"/>s and returns <see cref="InstallResult"/>s which contain
        /// new <see cref="IManagedTemplatesSource"/>, if update failed <see cref="InstallResult.Success"/> will be <c>false</c>.
        /// </summary>
        /// <param name="sources">List of <see cref="IManagedTemplatesSource"/> to be updated.</param>
        /// <returns>List of <see cref="InstallResult"/> with install information.</returns>
        Task<IReadOnlyList<InstallResult>> UpdateAsync(IEnumerable<IManagedTemplatesSourceUpdate> sources);

        /// <summary>
        /// Uninstalls specified <see cref="IManagedTemplatesSource"/>.
        /// </summary>
        /// <param name="source"><see cref="IManagedTemplatesSource"/> to be uninstalled.</param>
        /// <returns><see cref="UninstallResult"/> which has <see cref="UninstallResult.Success"/> which should be checked.</returns>
        Task<UninstallResult> UninstallAsync(IManagedTemplatesSource source);

        /// <summary>
        /// Installs new <see cref="IManagedTemplatesSource"/> based on <see cref="InstallRequest"/> data.
        /// All <see cref="IInstaller"/>s are considered via <see cref="IInstaller.CanInstallAsync(InstallRequest)"/> and if only 1 <see cref="IInstaller"/>
        /// returns <c>true</c>. <see cref="IInstaller.InstallAsync(InstallRequest)"/> is executed and result is returned.
        /// </summary>
        /// <param name="installRequest">Contains information needed to perform installation.</param>
        /// <returns><see cref="InstallResult"/> containing <see cref="IManagedTemplatesSource"/>, if <see cref="InstallResult.Success" /> is <c>true</c>.</returns>
        Task<InstallResult> InstallAsync(InstallRequest installRequest);

        /// <summary>
        /// Creates list of autocomplete candidates for <see cref="InstallRequest.Identifier"/>
        /// for all <see cref="IInstaller"/>s that are part of this <see cref="IManagedTemplatesSourcesProvider"/>.
        /// </summary>
        /// <param name="textSoFar">Text that was typed by user so far.</param>
        /// <param name="token">Calling code might call this on every key press, hence it is good idea to respect <see cref="CancellationToken"/></param>
        /// <returns>List of valid <see cref="InstallRequest.Identifier"/>s.</returns>
        Task<IReadOnlyList<string>> GetAutocompleteAsync(string textSoFar, CancellationToken token);
    }
}
