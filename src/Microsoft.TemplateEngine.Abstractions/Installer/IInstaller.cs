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

        IManagedTemplatesSource Deserialize(IManagedTemplatesSourcesProvider provider, TemplatesSourceData data);

        Task<IReadOnlyList<CheckUpdateResult>> GetLatestVersionAsync(IEnumerable<IManagedTemplatesSource> sources);

        Task<InstallResult> InstallAsync(InstallRequest installRequest);

        TemplatesSourceData Serialize(IManagedTemplatesSource managedSource);

        Task<UninstallResult> UninstallAsync(IManagedTemplatesSource managedSource);
    }
}
