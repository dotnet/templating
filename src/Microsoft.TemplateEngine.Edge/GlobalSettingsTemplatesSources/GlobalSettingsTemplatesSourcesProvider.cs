using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.GlobalSettings;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Edge
{
    public partial class GlobalSettingsTemplatesSourcesProviderFactory
    {
        internal class GlobalSettingsTemplatesSourcesProvider : IManagedTemplatesSourcesProvider
        {
            private readonly string PackagesFolder;
            private IEngineEnvironmentSettings _environmentSettings;
            private Dictionary<Guid, IInstaller> _installersByGuid = new Dictionary<Guid, IInstaller>();
            private Dictionary<string, IInstaller> _installersByName = new Dictionary<string, IInstaller>();
            private List<TemplatesSourceData> _notSupportedSources = new List<TemplatesSourceData>();
            private Dictionary<IInstaller, Dictionary<string, IManagedTemplatesSource>> _templatesSources = new Dictionary<IInstaller, Dictionary<string, IManagedTemplatesSource>>();

            public GlobalSettingsTemplatesSourcesProvider
                (GlobalSettingsTemplatesSourcesProviderFactory factory, IEngineEnvironmentSettings settings)
            {
                _ = factory ?? throw new ArgumentNullException(nameof(factory));
                _ = settings ?? throw new ArgumentNullException(nameof(settings));

                Factory = factory;
                PackagesFolder = Path.Combine(settings.Paths.TemplateEngineRootDir, "packages");
                if (!settings.Host.FileSystem.DirectoryExists(PackagesFolder))
                {
                    settings.Host.FileSystem.CreateDirectory(PackagesFolder);
                }

                _environmentSettings = settings;
                foreach (var installerFactory in settings.SettingsLoader.Components.OfType<IInstallerFactory>())
                {
                    var installer = installerFactory.CreateInstaller(this, settings, PackagesFolder);
                    _installersByName[installerFactory.Name] = installer;
                    _installersByGuid[installerFactory.Id] = installer;
                }

                ReloadCache();
                settings.SettingsLoader.GlobalSettings.SettingsChanged += ReloadCache;
            }

            public event Action SourcesChanged;

            public ITemplatesSourcesProviderFactory Factory { get; }

            public Task<IReadOnlyList<ITemplatesSource>> GetAllSourcesAsync(CancellationToken cancellationToken)
            {
                List<ITemplatesSource> templatesSources = new List<ITemplatesSource>();
                foreach (Dictionary<string, IManagedTemplatesSource> sourcesByInstaller in _templatesSources.Values)
                {
                    templatesSources.AddRange(sourcesByInstaller.Values);
                }
                templatesSources.AddRange(_notSupportedSources.Select(source => new TemplatesSource(this, source.MountPointUri, source.LastChangeTime)));
                return Task.FromResult((IReadOnlyList<ITemplatesSource>)templatesSources);
            }

            public async Task<IReadOnlyList<CheckUpdateResult>> GetLatestVersions(IEnumerable<IManagedTemplatesSource> sources)
            {
                _ = sources ?? throw new ArgumentNullException(nameof(sources));

                var tasks = new List<Task<IReadOnlyList<CheckUpdateResult>>>();
                foreach (var sourcesGroupedByInstaller in sources.GroupBy(s => s.Installer))
                {
                    tasks.Add(sourcesGroupedByInstaller.Key.GetLatestVersionAsync(sourcesGroupedByInstaller));
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);

                var result = new List<CheckUpdateResult>();
                foreach (var task in tasks)
                {
                    result.AddRange(task.Result);
                }
                return result;
            }

            public async Task<InstallResult> InstallAsync(InstallRequest installRequest)
            {
                _ = installRequest ?? throw new ArgumentNullException(nameof(installRequest));

                var installersThatCanInstall = new List<IInstaller>();
                foreach (var install in _installersByName.Values)
                {
                    if (await install.CanInstallAsync(installRequest).ConfigureAwait(false))
                    {
                        installersThatCanInstall.Add(install);
                    }
                }
                if (installersThatCanInstall.Count == 0)
                {
                    return InstallResult.CreateFailure(InstallerErrorCode.UnsupportedRequest, $"{installRequest.Identifier} cannot be installed");
                }

                IInstaller installer = installersThatCanInstall[0];
                return await InstallAsync(installRequest, installer).ConfigureAwait(false);
            }

            public async Task<UninstallResult> UninstallAsync(IManagedTemplatesSource source)
            {
                _ = source ?? throw new ArgumentNullException(nameof(source));

                UninstallResult result = await source.Installer.UninstallAsync(source).ConfigureAwait(false);
                if (result.Success)
                {
                    _templatesSources[source.Installer].Remove(source.Identifier);
                    _environmentSettings.SettingsLoader.GlobalSettings.Remove(source.Installer.Serialize(source));
                }
                return result;
            }

            public async Task<IReadOnlyList<UpdateResult>> UpdateAsync(IEnumerable<UpdateRequest> updateRequests)
            {
                _ = updateRequests ?? throw new ArgumentNullException(nameof(updateRequests));

                IEnumerable<UpdateRequest> updatesToApply = updateRequests.Where(request => request.Version != request.Source.Version);

                return await Task.WhenAll(updatesToApply.Select(async updateRequest => UpdateResult.FromInstallResult(updateRequest, await InstallAsync(updateRequest.Source, updateRequest.Version).ConfigureAwait(false)))).ConfigureAwait(false);
            }

            private async Task<InstallResult> InstallAsync(InstallRequest installRequest, IInstaller installer)
            {
                _ = installRequest ?? throw new ArgumentNullException(nameof(installRequest));
                _ = installer ?? throw new ArgumentNullException(nameof(installer));

                InstallResult installResult = await installer.InstallAsync(installRequest).ConfigureAwait(false);
                if (!installResult.Success)
                {
                    return installResult;
                }

                _templatesSources[installer][installResult.Source.Identifier] = installResult.Source;
                _environmentSettings.SettingsLoader.GlobalSettings.Add(installer.Serialize(installResult.Source));

                return installResult;
            }

            private Task<InstallResult> InstallAsync(IManagedTemplatesSource managedSource, string version)
            {
                _ = managedSource ?? throw new ArgumentNullException(nameof(managedSource));
                if (string.IsNullOrWhiteSpace(version))
                {
                    throw new ArgumentException("The argument cannot be null or empty", nameof(version));
                }

                InstallRequest installRequest = new InstallRequest
                {
                    Identifier = managedSource.Identifier,
                    Version = version
                };
                return InstallAsync(installRequest, managedSource.Installer);
            }

            private void ReloadCache()
            {
                _templatesSources = new Dictionary<IInstaller, Dictionary<string, IManagedTemplatesSource>>();
                foreach (IInstaller installer in _installersByGuid.Values)
                {
                    _templatesSources[installer] = new Dictionary<string, IManagedTemplatesSource>();
                }

                foreach (TemplatesSourceData entry in _environmentSettings.SettingsLoader.GlobalSettings.UserInstalledTemplatesSources)
                {
                    if (_installersByGuid.TryGetValue(entry.InstallerId, out var installer))
                    {
                        IManagedTemplatesSource managedTemplatesSource = installer.Deserialize(this, entry);
                        if (_templatesSources.TryGetValue(installer, out Dictionary<string, IManagedTemplatesSource> installerSources))
                        {
                            installerSources[managedTemplatesSource.Identifier] = managedTemplatesSource;
                        }
                    }
                    else
                    {
                        _notSupportedSources.Add(entry);
                    }
                }
                SourcesChanged?.Invoke();
            }
        }
    }
}
