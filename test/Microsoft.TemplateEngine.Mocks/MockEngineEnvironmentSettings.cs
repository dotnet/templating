using System;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Mocks
{
    public class MockEngineEnvironmentSettings : IEngineEnvironmentSettings
    {
        public ISettingsLoader SettingsLoader { get { throw new NotImplementedException(); } }

        public ITemplateEngineHost Host { get { throw new NotImplementedException(); } }

        public IEnvironment Environment { get { throw new NotImplementedException(); } }

        public IPathInfo Paths { get { throw new NotImplementedException(); } }

        public IJsonDocumentObjectModelFactory JsonDomFactory { get { throw new NotImplementedException(); } }
    }
}
