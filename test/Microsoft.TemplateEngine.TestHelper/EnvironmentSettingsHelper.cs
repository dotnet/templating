// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Utils;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
using System.Threading;

namespace Microsoft.TemplateEngine.TestHelper
{
    public class EnvironmentSettingsHelper : IDisposable
    {
        List<string> foldersToCleanup = new List<string>(0);

        public IEngineEnvironmentSettings CreateEnvironment(string locale = "en-US")
        {
            Environment.SetEnvironmentVariable(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "USERPROFILE" : "HOME", CreateTemporaryFolder());
            ITemplateEngineHost host = new TestHost
            {
                HostIdentifier = "TestRunner",
                Version = "1.0.0.0",
                Locale = locale,
                BuiltInComponents = new AssemblyComponentCatalog(new List<Assembly>()
                {
                    typeof(RunnableProjectGenerator).Assembly,//RunnableProject
                    typeof(SettingsLoader).Assembly//Edge
                }),
                FileSystem = new MonitoredFileSystem(new PhysicalFileSystem()),
                FallbackHostTemplateConfigNames = new[] { "dotnetcli" }
            };

            return new EngineEnvironmentSettings(host, (x) => new SettingsLoader(x));
        }

        public string CreateTemporaryFolder()
        {
            var folder = Path.Combine(Path.GetTempPath(), "DotnetNew3_Tests", Guid.NewGuid().ToString(), nameof(EnvironmentSettingsHelper));
            foldersToCleanup.Add(folder);
            Directory.CreateDirectory(folder);
            return folder;
        }

        public void Dispose()
        {
            try
            {
                foldersToCleanup.ForEach(f => Directory.Delete(f, true));
            }
            catch (Exception)
            {
                //Sometimes randomly fails :(
            }
        }
    }
}
