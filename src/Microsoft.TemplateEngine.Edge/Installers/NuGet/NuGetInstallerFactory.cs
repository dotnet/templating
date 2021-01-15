using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    class NuGetInstallerFactory : IInstallerFactory
    {
        public static readonly Guid FactoryId = new Guid("{015DCBAC-B4A5-49EA-94A6-061616EB60E2}");

        public Guid Id => FactoryId;

        public string Name => "NuGet";


        public IInstaller CreateInstaller(IManagedTemplatesSourcesProvider provider, IEngineEnvironmentSettings settings, string installPath)
        {
            return new NuGetInstaller(this, provider, settings, installPath);
        }
    }
}
