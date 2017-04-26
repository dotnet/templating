using System;
using System.IO;
using System.Text;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateEngine.Utils;
using Xunit;
using Microsoft.TemplateEngine.Mocks;

namespace Microsoft.TemplateEngine.Cli.UnitTests
{
    public abstract class TestBase
    {
        protected TestBase()
        {
            ITemplateEngineHost host = new TestHost
            {
                HostIdentifier = "TestRunner",
                Version = "1.0.0.0",
                Locale = "en-US"
            };

            EnvironmentSettings = new EngineEnvironmentSettings(host, s => new MockSettingsLoader());
        }

        public IEngineEnvironmentSettings EnvironmentSettings { get; }
        
    }
}
