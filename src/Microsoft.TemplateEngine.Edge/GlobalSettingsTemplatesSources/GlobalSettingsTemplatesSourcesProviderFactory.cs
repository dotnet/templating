using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge
{
    public partial class GlobalSettingsTemplatesSourcesProviderFactory : ITemplatesSourcesProviderFactory
    {
        public static readonly Guid FactoryId = new Guid("{3AACE22E-E978-4BAF-8BC1-568B290A238C}");

        public Guid Id => FactoryId;

        public string Name => "Global Settings";

        public ITemplatesSourcesProvider CreateProvider(IEngineEnvironmentSettings settings)
        {
            return new GlobalSettingsTemplatesSourcesProvider(this, settings);
        }
    }
}
