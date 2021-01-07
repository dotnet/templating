using Microsoft.TemplateEngine.Abstractions;
using System;

namespace Microsoft.TemplateEngine.Utils
{
    internal class OptionalWorkloadPackages : ITemplatesInstallSourcesProviderFactory
    {
        public static readonly Guid FactoryId = new Guid("{FAE2BB7C-054D-481B-B75C-E9F524193D56}");

        public Guid Id => FactoryId;

        public ITemplatesInstallSourcesProvider CreateProvider(IEngineEnvironmentSettings settings)
        {

        }
    }
}
