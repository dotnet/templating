// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.GlobalSettings;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesPackages;

namespace Microsoft.TemplateEngine.Edge.Installers.Folder
{
    internal class FolderInstaller : IInstaller
    {
        private readonly IEngineEnvironmentSettings _settings;

        public FolderInstaller(IEngineEnvironmentSettings settings, FolderInstallerFactory factory, IManagedTemplatesPackagesProvider provider)
        {
            Name = factory.Name;
            FactoryId = factory.Id;
            _settings = settings;
            Provider = provider;
        }

        public Guid FactoryId { get; }
        public string Name { get; }
        public IManagedTemplatesPackagesProvider Provider { get; }

        public Task<bool> CanInstallAsync(InstallRequest installationRequest, CancellationToken cancellationToken)
        {
            return Task.FromResult(Directory.Exists(installationRequest.Identifier));
        }

        public IManagedTemplatesPackage Deserialize(IManagedTemplatesPackagesProvider provider, TemplatesPackageData data)
        {
            return new FolderManagedTemplatesPackage(_settings, this, data.MountPointUri);
        }

        public Task<IReadOnlyList<CheckUpdateResult>> GetLatestVersionAsync(IEnumerable<IManagedTemplatesPackage> sources, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<CheckUpdateResult>>(sources.Select(s => CheckUpdateResult.CreateSuccess(s, null, true)).ToList());
        }

        public Task<InstallResult> InstallAsync(InstallRequest installRequest, CancellationToken cancellationToken)
        {
            if (Directory.Exists(installRequest.Identifier))
                return Task.FromResult(InstallResult.CreateSuccess(installRequest, new FolderManagedTemplatesPackage(_settings, this, installRequest.Identifier)));
            else
                return Task.FromResult(InstallResult.CreateFailure(installRequest, InstallerErrorCode.GenericError, null));
        }

        public TemplatesPackageData Serialize(IManagedTemplatesPackage managedSource)
        {
            return new TemplatesPackageData
            {
                MountPointUri = managedSource.MountPointUri,
                LastChangeTime = managedSource.LastChangeTime,
                InstallerId = FactoryId
            };
        }

        public Task<UninstallResult> UninstallAsync(IManagedTemplatesPackage managedSource, CancellationToken cancellationToken)
        {
            return Task.FromResult(UninstallResult.CreateSuccess(managedSource));
        }

        public Task<UpdateResult> UpdateAsync(UpdateRequest updateRequest, CancellationToken cancellationToken)
        {
            return Task.FromResult(UpdateResult.CreateSuccess(updateRequest, updateRequest.Source));
        }
    }
}
