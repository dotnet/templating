using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
using Microsoft.TemplateEngine.Abstractions.TemplatesPackages;
using Microsoft.TemplateEngine.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.EndToEndTestHarness
{
    class BuiltInTemplatesPackagesProviderFactory : ITemplatesPackagesProviderFactory
    {
        public string Name => "E2E Harness BuiltIn";

        public Guid Id { get; } = new Guid("{3227D09D-C1EA-48F1-A33B-1F132BFD9F00}");

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

                string codebase = typeof(Program).GetTypeInfo().Assembly.Location;
                Uri cb = new Uri(codebase);
                string asmPath = cb.LocalPath;
                string dir = Path.GetDirectoryName(asmPath);
                string[] locations = new[]
                {
                    Path.Combine(dir, "..", "..", "..", "..", "..", "template_feed"),
                    Path.Combine(dir, "..", "..", "..", "..", "..", "test", "Microsoft.TemplateEngine.TestTemplates", "test_templates")
                };

                foreach (string location in locations)
                {
                    IFileLastWriteTimeSource fileSystem = _settings.Host.FileSystem as IFileLastWriteTimeSource;
                    IEnumerable<string> expandedPaths = InstallRequestPathResolution.Expand(location, _settings);
                    templatesPackages.AddRange(expandedPaths.Select(path => new TemplatesPackage(this, path, fileSystem?.GetLastWriteTimeUtc(path) ?? File.GetLastWriteTime(path))));
                }

                return Task.FromResult((IReadOnlyList<ITemplatesPackage>)templatesPackages);
            }
        }
    }
}
