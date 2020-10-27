using Microsoft.TemplateEngine.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Mocks
{
    public class MockEngineEnvironmentSettings : IEngineEnvironmentSettings
    {
        public ISettingsLoader SettingsLoader { get { throw new NotImplementedException(); } }

        public ITemplateEngineHost Host { get; set; }

        public IEnvironment Environment { get; set; }

        public IPathInfo Paths { get { throw new NotImplementedException(); } }
    }

    public class MockEnvironment : IEnvironment
    {
        public string NewLine => Environment.NewLine;

        public int ConsoleBufferWidth
        {
            get; set;
        }

        public string ExpandEnvironmentVariables(string name)
        {
            throw new NotImplementedException();
        }

        public string GetEnvironmentVariable(string name)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyDictionary<string, string> GetEnvironmentVariables()
        {
            throw new NotImplementedException();
        }
    }
}
