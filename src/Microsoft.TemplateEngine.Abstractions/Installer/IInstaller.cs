// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions.GlobalSettings;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;

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

        IManagedTemplatesSourcesProvider Provider { get; }

        /// <summary>
        /// Installer should determine if it can install specific <see cref="InstallRequest"/>.
        /// Ideally it should as far as calling backend server to determine if such identifier exists.
        /// </summary>
        Task<bool> CanInstallAsync(InstallRequest installationRequest);

        /// <summary>
        /// Deserializes <see cref="TemplatesSourceData"/> to <see cref="IManagedTemplatesSource"/>
        /// </summary>
        /// <param name="provider">The provider that provides the data</param>
        /// <param name="data">Data to serialize</param>
        /// <returns>deserialized <see cref="IManagedTemplatesSource"/></returns>
        IManagedTemplatesSource Deserialize(IManagedTemplatesSourcesProvider provider, TemplatesSourceData data);

        /// <summary>
        /// Gets latest versions for <paramref name="sources"/>
        /// </summary>
        /// <param name="sources">sources to get latest versions for</param>
        /// <returns>list of <see cref="CheckUpdateResult"/> containing latest versions for the sources</returns>
        Task<IReadOnlyList<CheckUpdateResult>> GetLatestVersionAsync(IEnumerable<IManagedTemplatesSource> sources);

        /// <summary>
        /// Installs the template source
        /// </summary>
        /// <param name="installRequest">details for installation</param>
        /// <returns><see cref="InstallResult"/> containing installation results and <see cref="IManagedTemplatesSource"/> if installation was successful</returns>
        Task<InstallResult> InstallAsync(InstallRequest installRequest);

        /// <summary>
        /// Serializes <see cref="IManagedTemplatesSource"/> to <see cref="TemplatesSourceData"/>
        /// </summary>
        /// <param name="managedSource">source to serialize </param>
        /// <returns>serialized <see cref="TemplatesSourceData"/></returns>
        TemplatesSourceData Serialize(IManagedTemplatesSource managedSource);

        /// <summary>
        /// Uninstalls the template source
        /// </summary>
        /// <param name="managedSource">source to uninstall</param>
        /// <returns><see cref="UninstallResult"/> containing uninstallation result</returns>
        Task<UninstallResult> UninstallAsync(IManagedTemplatesSource managedSource);

        /// <summary>
        /// Updates the template source
        /// </summary>
        /// <param name="updateRequest"><see cref="UpdateRequest"/> defining source to update and target version</param>
        /// <returns><see cref="UpdateResult"/> containing update results and <see cref="IManagedTemplatesSource"/> if update was successful</returns>
        Task<UpdateResult> UpdateAsync(UpdateRequest updateRequest);
    }
}
