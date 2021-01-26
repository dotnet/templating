using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge
{
    public partial class GlobalSettingsTemplatesSourcesProviderFactory
    {
        internal class GlobalSettingsTemplatesSourcesProvider : IManagedTemplatesSourcesProvider
        {
            private readonly string PackagesFolder;
            private IEngineEnvironmentSettings settings;
            private Dictionary<Guid, IInstaller> installersByGuid = new Dictionary<Guid, IInstaller>();
            private Dictionary<string, IInstaller> installersByName = new Dictionary<string, IInstaller>();

            public GlobalSettingsTemplatesSourcesProvider
                (GlobalSettingsTemplatesSourcesProviderFactory factory, IEngineEnvironmentSettings settings)
            {
                Factory = factory;
                PackagesFolder = Path.Combine(settings.Paths.TemplateEngineRootDir, "packages");
                this.settings = settings;
                foreach (var installerFactory in settings.SettingsLoader.Components.OfType<IInstallerFactory>())
                {
                    var installer = installerFactory.CreateInstaller(this, settings, PackagesFolder);
                    installersByName[installerFactory.Name] = installer;
                    installersByGuid[installerFactory.Id] = installer;
                }
                settings.SettingsLoader.GlobalSettings.SettingsChanged += SourcesChanged;
            }

            public ITemplatesSourcesProviderFactory Factory { get; }

            public event Action SourcesChanged;

            public Task<IReadOnlyList<ITemplatesSource>> GetAllSourcesAsync(CancellationToken cancellationToken)
            {
                var list = new List<ITemplatesSource>();
                var userSettings = settings.SettingsLoader.GlobalSettings;
                foreach (var entry in userSettings.UserInstalledTemplatesSources)
                {
                    if(installersByGuid.TryGetValue(entry.InstallerId, out var installer))
                    {
                        if(installer.Deserialize (this, entry.MountPointUri, entry.Details) is IManagedTemplatesSource managedSource)
                        {
                            list.Add(managedSource);
                        }
                    }
                }
                return Task.FromResult<IReadOnlyList<ITemplatesSource>>(list);
            }

            public async Task<IReadOnlyList<string>> GetAutocompleteAsync(string textSoFar, CancellationToken token)
            {
                var tasks = new List<Task<IReadOnlyList<string>>>();
                foreach (var installer in installersByGuid.Values)
                {
                    tasks.Add(installer.GetAutocompleteAsync(textSoFar, token));
                }
                var result = new List<string>();
                foreach (var task in tasks)
                {
                    result.AddRange(await task.ConfigureAwait(false));
                }
                return result.Distinct().OrderBy(e => e).ToList();
            }

            public Task<IReadOnlyList<IManagedTemplatesSourceUpdate>> GetLatestVersions(IEnumerable<IManagedTemplatesSource> sources)
            {
                throw new NotImplementedException();
            }

            public async Task<InstallResult> InstallAsync(InstallRequest installRequest)
            {
                var installersThatCanInstall = new List<IInstaller>();
                foreach (var install in installersByName.Values)
                {
                    if (await install.CanInstallAsync(installRequest))
                    {
                        installersThatCanInstall.Add(install);
                    }
                }
                if (installersThatCanInstall.Count == 0)
                    return InstallResult.CreateFailure("");
                if (installersThatCanInstall.Count > 1)
                    return InstallResult.CreateFailure("");
                var installer = installersThatCanInstall[0];
                var installResult = await installer.InstallAsync(installRequest);

                if (!installResult.Success)
                    return installResult;

                var data = installer.Serialize(installResult.ManagedTemplateSource);
                settings.SettingsLoader.GlobalSettings.Add(new Abstractions.GlobalSettings.TemplatesSourceData()
                {
                    Details = data.details,
                    InstallerId = installer.FactoryId,
                    LastChangeTime = DateTime.Now,
                    MountPointUri = data.mountPointUri
                });
                return installResult;
            }

            public Task<UninstallResult> UninstallAsync(IManagedTemplatesSource source)
            {
                throw new NotImplementedException();
            }

            public Task<IReadOnlyList<InstallResult>> UpdateAsync(IEnumerable<IManagedTemplatesSourceUpdate> sources)
            {
                throw new NotImplementedException();
            }
        }
    }
}
