using Microsoft.DotNet.TemplateLocator;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
using Microsoft.TemplateEngine.Cli;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Mount.Archive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Utils
{
    internal class OptionalWorkloadPackages : ITemplatesInstallSourcesProviderFactory
    {
        public static readonly Guid FactoryId = new Guid("{FAE2BB7C-054D-481B-B75C-E9F524193D56}");

        public Guid Id => FactoryId;

        public ITemplatesInstallSourcesProvider CreateProvider(IEngineEnvironmentSettings settings)
        {
            return new OptionalWorkloadProvider(settings);
        }

        class OptionalWorkloadProvider : ITemplatesInstallSourcesProvider
        {
            private IEngineEnvironmentSettings EnvironmentSettings;

            public OptionalWorkloadProvider(IEngineEnvironmentSettings settings)
            {
                this.EnvironmentSettings = settings;
            }

            public Task<List<TemplatesInstallSource>> GetInstalledPackagesAsync(CancellationToken cancellationToken)
            {
                string sdkVersion = EnvironmentSettings.Host.Version.Substring(1); // Host.Version (from SDK) has a leading "v" that need to remove.
                try
                {
                    var list = new List<TemplatesInstallSource>();
                    var _paths = new Paths(EnvironmentSettings);
                    TemplateLocator optionalWorkloadLocator = new TemplateLocator();
                    string dotnetPath = Path.GetDirectoryName(Path.GetDirectoryName(_paths.Global.BaseDir));

                    var packages = optionalWorkloadLocator.GetDotnetSdkTemplatePackages(sdkVersion, dotnetPath);
                    var fileSystem = (EnvironmentSettings.Host.FileSystem as IFileLastWriteTimeSource);
                    foreach (IOptionalSdkTemplatePackageInfo packageInfo in packages)
                    {
                        list.Add(new TemplatesInstallSource()
                        {
                            MountPointFactoryId = ZipFileMountPointFactory.FactoryId,
                            Place = packageInfo.Path,
                            LastWriteTime = fileSystem?.GetLastWriteTimeUtc(packageInfo.Path) ?? File.GetLastWriteTime(packageInfo.Path)
                        });
                    }
                    return Task.FromResult(list);
                }
                catch (Exception ex)
                {
                    throw new HiveSynchronizationException(LocalizableStrings.OptionalWorkloadsSyncFailed, sdkVersion, ex);
                }
            }
        }
    }
}
