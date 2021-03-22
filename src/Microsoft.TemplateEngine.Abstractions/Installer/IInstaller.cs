// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions.GlobalSettings;
using Microsoft.TemplateEngine.Abstractions.TemplatesPackages;

namespace Microsoft.TemplateEngine.Abstractions.Installer
{
    public interface IInstaller
    {
        /// <summary>
        /// User can specify name of specific installer to be used to install package.
        /// e.g: nuget, folder, vsix(to download from VS marketplace), npm, maven...
        /// This is useful when installer can't be determined based on <see cref="InstallRequest.Identifier"/> and <see cref="InstallRequest.Details"/>
        /// </summary>
        Guid FactoryId { get; }

        /// <summary>
        /// User can specify name of specific installer to be used to install package.
        /// e.g: nuget, folder, vsix(to download from VS marketplace), npm, maven...
        /// This is useful when installer can't be determined based on <see cref="InstallRequest.Identifier"/> and <see cref="InstallRequest.Details"/>
        /// </summary>
        string Name { get; }

        IManagedTemplatesPackagesProvider Provider { get; }

        /// <summary>
        /// Installer should determine if it can install specific <see cref="InstallRequest"/>.
        /// Ideally it should as far as calling backend server to determine if such identifier exists.
        /// </summary>
        Task<bool> CanInstallAsync(InstallRequest installationRequest, CancellationToken cancellationToken);

        /// <summary>
        /// Deserializes <see cref="TemplatesPackageData"/> to <see cref="IManagedTemplatesPackage"/>
        /// </summary>
        /// <param name="provider">The provider that provides the data</param>
        /// <param name="data">Data to serialize</param>
        /// <returns>deserialized <see cref="IManagedTemplatesPackage"/></returns>
        IManagedTemplatesPackage Deserialize(IManagedTemplatesPackagesProvider provider, TemplatesPackageData data);

        /// <summary>
        /// Gets latest versions for <paramref name="sources"/>
        /// </summary>
        /// <param name="sources">sources to get latest versions for</param>
        /// <returns>list of <see cref="CheckUpdateResult"/> containing latest versions for the sources</returns>
        /// <param name="cancellationToken"></param>
        Task<IReadOnlyList<CheckUpdateResult>> GetLatestVersionAsync(IEnumerable<IManagedTemplatesPackage> sources, CancellationToken cancellationToken);

        /// <summary>
        /// Installs the template source
        /// </summary>
        /// <param name="installRequest">details for installation</param>
        /// <returns><see cref="InstallResult"/> containing installation results and <see cref="IManagedTemplatesPackage"/> if installation was successful</returns>
        /// <param name="cancellationToken"></param>
        Task<InstallResult> InstallAsync(InstallRequest installRequest, CancellationToken cancellationToken);

        /// <summary>
        /// Serializes <see cref="IManagedTemplatesPackage"/> to <see cref="TemplatesPackageData"/>
        /// </summary>
        /// <param name="managedSource">source to serialize </param>
        /// <returns>serialized <see cref="TemplatesPackageData"/></returns>
        TemplatesPackageData Serialize(IManagedTemplatesPackage managedSource);

        /// <summary>
        /// Uninstalls the template source
        /// </summary>
        /// <param name="managedSource">source to uninstall</param>
        /// <returns><see cref="UninstallResult"/> containing uninstallation result</returns>
        /// <param name="cancellationToken"></param>
        Task<UninstallResult> UninstallAsync(IManagedTemplatesPackage managedSource, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the template source
        /// </summary>
        /// <param name="updateRequest"><see cref="UpdateRequest"/> defining source to update and target version</param>
        /// <returns><see cref="UpdateResult"/> containing update results and <see cref="IManagedTemplatesPackage"/> if update was successful</returns>
        /// <param name="cancellationToken"></param>
        Task<UpdateResult> UpdateAsync(UpdateRequest updateRequest, CancellationToken cancellationToken);
    }
}
