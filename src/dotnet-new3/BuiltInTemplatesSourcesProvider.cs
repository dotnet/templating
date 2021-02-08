using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
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
    class BuiltInTemplatesSourcesProviderFactory : ITemplatesSourcesProviderFactory
    {
        public string Name => "new3 BuiltIn";

        public Guid Id { get; } = new Guid("{3227D09D-C1EA-48F1-A33B-1F132BFD9F06}");

        public ITemplatesSourcesProvider CreateProvider(IEngineEnvironmentSettings settings)
        {
            return new BuiltInTemplatesSourcesProvider(this, settings);
        }

        class BuiltInTemplatesSourcesProvider : ITemplatesSourcesProvider
        {
            private readonly IEngineEnvironmentSettings _settings;

            public BuiltInTemplatesSourcesProvider(BuiltInTemplatesSourcesProviderFactory factory, IEngineEnvironmentSettings settings)
            {
                _settings = settings;
                Factory = factory;
            }

            public ITemplatesSourcesProviderFactory Factory { get; }

            event Action ITemplatesSourcesProvider.SourcesChanged
            {
                add { }
                remove { }
            }

            public Task<IReadOnlyList<ITemplatesSource>> GetAllSourcesAsync(CancellationToken cancellationToken)
            {
                List<ITemplatesSource> templatesSources = new List<ITemplatesSource>();

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
                        return Task.FromResult((IReadOnlyList<ITemplatesSource>)templatesSources);
                    }
                    Environment.SetEnvironmentVariable("DN3", path);
                }

                Paths paths = new Paths(_settings);

                if (paths.FileExists(paths.Global.DefaultInstallTemplateList))
                {
                    IFileLastWriteTimeSource fileSystem = _settings.Host.FileSystem as IFileLastWriteTimeSource;
                    foreach (string sourceLocation in paths.ReadAllText(paths.Global.DefaultInstallTemplateList).Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string expandedPath = Environment.ExpandEnvironmentVariables(sourceLocation);
                        IEnumerable<string> expandedPaths = InstallRequestPathResolution.Expand(expandedPath, _settings);
                        templatesSources.AddRange(expandedPaths.Select(path => new TemplatesSource(this, path, fileSystem?.GetLastWriteTimeUtc(path) ?? File.GetLastWriteTime(path))));
                    }
                }

                return Task.FromResult((IReadOnlyList<ITemplatesSource>)templatesSources);
            }
        }
    }
}
