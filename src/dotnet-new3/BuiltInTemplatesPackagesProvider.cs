using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
using Microsoft.TemplateEngine.Abstractions.TemplatesPackages;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace dotnet_new3
{
    class BuiltInTemplatesPackagesProviderFactory : ITemplatesPackagesProviderFactory
    {
        public string Name => "new3 BuiltIn";

        public Guid Id { get; } = new Guid("{3227D09D-C1EA-48F1-A33B-1F132BFD9F06}");

        public ITemplatesPackagesProvider CreateProvider(IEngineEnvironmentSettings settings)
        {
            return new BuiltInTemplatesPackagesProvider(this, settings);
        }

        class BuiltInTemplatesPackagesProvider : ITemplatesPackagesProvider
        {
            private readonly IEngineEnvironmentSettings _settings;

            public BuiltInTemplatesPackagesProvider(BuiltInTemplatesPackagesProviderFactory factory, IEngineEnvironmentSettings settings)
            {
                _settings = settings;
                Factory = factory;
            }

            public ITemplatesPackagesProviderFactory Factory { get; }

            event Action ITemplatesPackagesProvider.SourcesChanged
            {
                add { }
                remove { }
            }

            public Task<IReadOnlyList<ITemplatesPackage>> GetAllSourcesAsync(CancellationToken cancellationToken)
            {
                List<ITemplatesPackage> templatesPackages = new List<ITemplatesPackage>();

                string dn3Path = _settings.Environment.GetEnvironmentVariable("DN3");
                if (string.IsNullOrEmpty(dn3Path))
                {
                    string path = typeof(Program).Assembly.Location;
                    while (path != null && !File.Exists(Path.Combine(path, "Microsoft.TemplateEngine.sln")))
                    {
                        path = Path.GetDirectoryName(path);
                    }
                    if (path == null)
                    {
                        _settings.Host.LogDiagnosticMessage("Couldn't the setup package location, because \"Microsoft.TemplateEngine.sln\" is not in any of parent directories.", "Install");
                        return Task.FromResult((IReadOnlyList<ITemplatesPackage>)templatesPackages);
                    }
                    Environment.SetEnvironmentVariable("DN3", path);
                }

                Paths paths = new Paths(_settings);

                if (paths.FileExists(paths.Global.DefaultInstallTemplateList))
                {
                    IFileLastWriteTimeSource fileSystem = _settings.Host.FileSystem as IFileLastWriteTimeSource;
                    foreach (string sourceLocation in paths.ReadAllText(paths.Global.DefaultInstallTemplateList).Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string expandedPath = Environment.ExpandEnvironmentVariables(sourceLocation).Replace('\\', Path.DirectorySeparatorChar);
                        IEnumerable<string> expandedPaths = InstallRequestPathResolution.Expand(expandedPath, _settings);
                        templatesPackages.AddRange(expandedPaths.Select(path => new TemplatesPackage(this, path, fileSystem?.GetLastWriteTimeUtc(path) ?? File.GetLastWriteTime(path))));
                    }
                }

                return Task.FromResult((IReadOnlyList<ITemplatesPackage>)templatesPackages);
            }
        }
    }
}
