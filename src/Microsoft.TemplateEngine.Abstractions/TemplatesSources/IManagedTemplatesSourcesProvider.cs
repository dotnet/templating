using Microsoft.TemplateEngine.Abstractions.Installer;
using System.Collections.Generic;
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
        /// And returns list of <see cref="CheckUpdateResult"/> which contains original <see cref="IManagedTemplatesSource"/>
        /// so caller can compare <see cref="CheckUpdateResult.Version"/> with <see cref="IManagedTemplatesSource.Version"/>
        /// </summary>
        /// <param name="managedSources">List of <see cref="IManagedTemplatesSource"/> to get latest version for.</param>
        /// <returns>List of <see cref="ManagedTemplatesSourceUpdate"/></returns>
        Task<IReadOnlyList<CheckUpdateResult>> GetLatestVersions(IEnumerable<IManagedTemplatesSource> managedSources);

        /// <summary>
        /// Updates specified <see cref="IManagedTemplatesSource"/>s and returns <see cref="UpdateResult"/>s which contain
        /// new <see cref="IManagedTemplatesSource"/>, if update failed <see cref="UpdateResult.Success"/> will be <c>false</c>.
        /// </summary>
        /// <param name="updateRequests">List of <see cref="IManagedTemplatesSource"/> to be updated.</param>
        /// <returns>List of <see cref="InstallResult"/> with install information.</returns>
        Task<IReadOnlyList<UpdateResult>> UpdateAsync(IEnumerable<UpdateRequest> updateRequests);

        /// <summary>
        /// Uninstalls specified <see cref="IManagedTemplatesSource"/>.
        /// </summary>
        /// <param name="managedSources">list of <see cref="IManagedTemplatesSource"/>s to be uninstalled.</param>
        /// <returns><see cref="UninstallResult"/> which has <see cref="UninstallResult.Success"/> which should be checked.</returns>
        Task<IReadOnlyList<UninstallResult>> UninstallAsync(IEnumerable<IManagedTemplatesSource> managedSources);

        /// <summary>
        /// Installs new <see cref="IManagedTemplatesSource"/> based on <see cref="InstallRequest"/> data.
        /// All <see cref="IInstaller"/>s are considered via <see cref="IInstaller.CanInstallAsync(InstallRequest)"/> and if only 1 <see cref="IInstaller"/>
        /// returns <c>true</c>. <see cref="IInstaller.InstallAsync(InstallRequest)"/> is executed and result is returned.
        /// </summary>
        /// <param name="installRequests">Contains the list of install requests to perform.</param>
        /// <returns><see cref="InstallResult"/> containing <see cref="IManagedTemplatesSource"/>, if <see cref="InstallResult.Success" /> is <c>true</c>.</returns>
        Task<IReadOnlyList<InstallResult>> InstallAsync(IEnumerable<InstallRequest> installRequests);
    }
}
