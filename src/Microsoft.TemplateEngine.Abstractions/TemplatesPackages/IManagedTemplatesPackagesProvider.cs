using Microsoft.TemplateEngine.Abstractions.Installer;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Abstractions.TemplatesPackages
{
    /// <summary>
    /// This provider is responsible for managing <see cref="IManagedTemplatesPackage"/>.
    /// </summary>
    public interface IManagedTemplatesPackagesProvider : ITemplatesPackagesProvider
    {
        /// <summary>
        /// Takes list of <see cref="IManagedTemplatesPackage"/> as input so it can check for latest versions in batch.
        /// And returns list of <see cref="CheckUpdateResult"/> which contains original <see cref="IManagedTemplatesPackage"/>
        /// so caller can compare <see cref="CheckUpdateResult.LatestVersion"/> with <see cref="IManagedTemplatesPackage.Version"/>
        /// </summary>
        /// <param name="managedSources">List of <see cref="IManagedTemplatesPackage"/> to get latest version for.</param>
        /// <returns>List of <see cref="ManagedTemplatesPackageUpdate"/></returns>
        /// <param name="cancellationToken"></param>
        Task<IReadOnlyList<CheckUpdateResult>> GetLatestVersionsAsync(IEnumerable<IManagedTemplatesPackage> managedSources, CancellationToken cancellationToken);

        /// <summary>
        /// Updates specified <see cref="IManagedTemplatesPackage"/>s and returns <see cref="UpdateResult"/>s which contain
        /// new <see cref="IManagedTemplatesPackage"/>, if update failed <see cref="UpdateResult.Success"/> will be <c>false</c>.
        /// </summary>
        /// <param name="updateRequests">List of <see cref="IManagedTemplatesPackage"/> to be updated.</param>
        /// <returns>List of <see cref="UpdateResult"/> with install information.</returns>
        /// <param name="cancellationToken"></param>
        Task<IReadOnlyList<UpdateResult>> UpdateAsync(IEnumerable<UpdateRequest> updateRequests, CancellationToken cancellationToken);

        /// <summary>
        /// Uninstalls specified <see cref="IManagedTemplatesPackage"/>.
        /// </summary>
        /// <param name="managedSources">list of <see cref="IManagedTemplatesPackage"/>s to be uninstalled.</param>
        /// <returns><see cref="UninstallResult"/> which has <see cref="UninstallResult.Success"/> which should be checked.</returns>
        /// <param name="cancellationToken"></param>
        Task<IReadOnlyList<UninstallResult>> UninstallAsync(IEnumerable<IManagedTemplatesPackage> managedSources, CancellationToken cancellationToken);

        /// <summary>
        /// Installs new <see cref="IManagedTemplatesPackage"/> based on <see cref="InstallRequest"/> data.
        /// All <see cref="IInstaller"/>s are considered via <see cref="IInstaller.CanInstallAsync(InstallRequest, CancellationToken)"/> and if only 1 <see cref="IInstaller"/>
        /// returns <c>true</c>. <see cref="IInstaller.InstallAsync(InstallRequest, CancellationToken)"/> is executed and result is returned.
        /// </summary>
        /// <param name="installRequests">Contains the list of install requests to perform.</param>
        /// <returns><see cref="InstallResult"/> containing <see cref="IManagedTemplatesPackage"/>, if <see cref="InstallResult.Success" /> is <c>true</c>.</returns>
        /// <param name="cancellationToken"></param>
        Task<IReadOnlyList<InstallResult>> InstallAsync(IEnumerable<InstallRequest> installRequests, CancellationToken cancellationToken);
    }
}
