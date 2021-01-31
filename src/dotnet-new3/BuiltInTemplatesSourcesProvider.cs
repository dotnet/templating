using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using Microsoft.TemplateEngine.Edge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            private readonly IEngineEnvironmentSettings settings;

            public BuiltInTemplatesSourcesProvider(BuiltInTemplatesSourcesProviderFactory factory, IEngineEnvironmentSettings settings)
            {
                this.settings = settings;
                this.Factory = factory;
            }

            public ITemplatesSourcesProviderFactory Factory { get; }

            public event Action SourcesChanged;

            public async Task<IReadOnlyList<ITemplatesSource>> GetAllSourcesAsync(CancellationToken cancellationToken)
            {
                List<ITemplatesSource> toInstallList = new List<ITemplatesSource>();

                string dn3Path = settings.Environment.GetEnvironmentVariable("DN3");
                if (string.IsNullOrEmpty(dn3Path))
                {
                    string path = typeof(Program).Assembly.Location;
                    while (path != null && !File.Exists(Path.Combine(path, "Microsoft.TemplateEngine.sln")))
                    {
                        path = Path.GetDirectoryName(path);
                    }
                    if (path == null)
                    {
                        settings.Host.LogDiagnosticMessage("Couldn't the setup package location, because \"Microsoft.TemplateEngine.sln\" is not in any of parent directories.", "Install");
                        return toInstallList;
                    }
                    Environment.SetEnvironmentVariable("DN3", path);
                }

                Paths paths = new Paths(settings);

                if (paths.FileExists(paths.Global.DefaultInstallTemplateList))
                {
                    var fileSystem = settings.Host.FileSystem as IFileLastWriteTimeSource;
                    foreach (var nupkg in paths.ReadAllText(paths.Global.DefaultInstallTemplateList).Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var expandedPath = Environment.ExpandEnvironmentVariables(nupkg).TrimEnd('\\').TrimEnd('/');
                        toInstallList.Add(new TemplatesSource(this, expandedPath, fileSystem?.GetLastWriteTimeUtc(expandedPath) ?? File.GetLastWriteTime(expandedPath)));
                    }
                }

                return toInstallList;
            }
        }
    }
}
