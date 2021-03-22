using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatesPackages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.IDE.IntegrationTests.Utils
{
    class BuiltInTemplatesPackagesProviderFactory : ITemplatesPackagesProviderFactory
    {
        public string Name => "IDE.IntegrationTests BuiltIn";

        public Guid Id { get; } = new Guid("{3227D09D-C1EA-48F1-A33B-1F132BFD9F01}");

        public ITemplatesPackagesProvider CreateProvider(IEngineEnvironmentSettings settings)
        {
            return new BuiltInTemplatesPackagesProvider(this, settings);
        }

        class BuiltInTemplatesPackagesProvider : ITemplatesPackagesProvider
        {
            private readonly IEngineEnvironmentSettings settings;

            public BuiltInTemplatesPackagesProvider(BuiltInTemplatesPackagesProviderFactory factory, IEngineEnvironmentSettings settings)
            {
                this.settings = settings;
                this.Factory = factory;
            }

            public ITemplatesPackagesProviderFactory Factory { get; }

            event Action ITemplatesPackagesProvider.SourcesChanged
            {
                add { }
                remove { }
            }

            public Task<IReadOnlyList<ITemplatesPackage>> GetAllSourcesAsync(CancellationToken cancellationToken)
            {
                List<ITemplatesPackage> toInstallList = new List<ITemplatesPackage>();
                string codebase = typeof(BootstrapperFactory).GetTypeInfo().Assembly.Location;
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
                    if (Directory.Exists(location))
                    {
                        toInstallList.Add(new TemplatesPackage(this, new DirectoryInfo(location).FullName, File.GetLastWriteTime(location)));
                    }
                }
                return Task.FromResult((IReadOnlyList<ITemplatesPackage>)toInstallList);
            }
        }
    }
}
