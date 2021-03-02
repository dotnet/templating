// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.GlobalSettings;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;

namespace Microsoft.TemplateEngine.Edge.Installers.Folder
{
    internal class FolderInstaller : IInstaller
    {
        private readonly IEngineEnvironmentSettings _settings;

        public FolderInstaller(IEngineEnvironmentSettings settings, FolderInstallerFactory factory, IManagedTemplatesSourcesProvider provider)
        {
            Name = factory.Name;
            FactoryId = factory.Id;
            _settings = settings;
            Provider = provider;
        }

        public Guid FactoryId { get; }
        public string Name { get; }
        public IManagedTemplatesSourcesProvider Provider { get; }

        public Task<bool> CanInstallAsync(InstallRequest installationRequest)
        {
            return Task.FromResult(Directory.Exists(installationRequest.Identifier));
        }

        public IManagedTemplatesSource Deserialize(IManagedTemplatesSourcesProvider provider, TemplatesSourceData data)
        {
            return new FolderManagedTemplatesSource(_settings, provider, data.MountPointUri);
        }

        public Task<IReadOnlyList<CheckUpdateResult>> GetLatestVersionAsync(IEnumerable<IManagedTemplatesSource> sources)
        {
            return Task.FromResult<IReadOnlyList<CheckUpdateResult>>(sources.Select(s => CheckUpdateResult.CreateSuccess(s, null)).ToList());
        }

        public Task<InstallResult> InstallAsync(InstallRequest installRequest)
        {
            if (Directory.Exists(installRequest.Identifier))
                return Task.FromResult(InstallResult.CreateSuccess(installRequest, new FolderManagedTemplatesSource(_settings, Provider, installRequest.Identifier)));
            else
                return Task.FromResult(InstallResult.CreateFailure(installRequest, InstallerErrorCode.GenericError, null));
        }

        public TemplatesSourceData Serialize(IManagedTemplatesSource managedSource)
        {
            return new TemplatesSourceData
            {
                MountPointUri = managedSource.MountPointUri,
                LastChangeTime = managedSource.LastChangeTime,
                InstallerId = FactoryId
            };
        }

        public Task<UninstallResult> UninstallAsync(IManagedTemplatesSource managedSource)
        {
            return Task.FromResult(UninstallResult.CreateSuccess(managedSource));
        }

        public Task<UpdateResult> UpdateAsync(UpdateRequest updateRequest)
        {
            return Task.FromResult(UpdateResult.CreateSuccess(updateRequest, updateRequest.Source));
        }
    }
}
