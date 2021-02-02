using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.IDE.IntegrationTests.Utils
{
    class BuiltInTemplatesSourcesProviderFactory : ITemplatesSourcesProviderFactory
    {
        public string Name => "IDE.IntegrationTests BuiltIn";

        public Guid Id { get; } = new Guid("{3227D09D-C1EA-48F1-A33B-1F132BFD9F01}");

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

            public Task<IReadOnlyList<ITemplatesSource>> GetAllSourcesAsync(CancellationToken cancellationToken)
            {
                List<ITemplatesSource> toInstallList = new List<ITemplatesSource>();

                string codebase = typeof(BootstrapperFactory).GetTypeInfo().Assembly.Location;
                Uri cb = new Uri(codebase);
                string asmPath = cb.LocalPath;
                string dir = Path.GetDirectoryName(asmPath);
                string[] locations = new []
                {
                    Path.Combine(dir, "..", "..", "..", "..", "..", "template_feed"),
                    Path.Combine(dir, "..", "..", "..", "..", "..", "test", "Microsoft.TemplateEngine.TestTemplates", "test_templates"),
                    Path.Combine(dir, "..", "..", "..", "..", "..", "test", "Microsoft.TemplateEngine.TestTemplates", "test_templates")
                };

                foreach (string location in locations)
                {
                    if (Directory.Exists(location))
                    {
                        toInstallList.Add(new TemplatesSource(this, new DirectoryInfo(location).FullName, File.GetLastWriteTime(location)));
                    }
                }
                return Task.FromResult((IReadOnlyList<ITemplatesSource>)toInstallList);
            }
        }
    }
}
