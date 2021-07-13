// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.NET.TestFramework.Commands;
using Microsoft.TemplateEngine.TestHelper;
using Xunit.Abstractions;

namespace Microsoft.TemplateSearch.TemplateDiscovery.IntegrationTests
{
    public class DotnetNew3Command : TestCommand
    {
        private bool _hiveSet;

        public DotnetNew3Command(ITestOutputHelper log, params string[] args) : base(log)
        {
            // Set dotnet-new3.dll as first Argument to be passed to "dotnet"
            // And use full path since we want to execute in any working directory
            Arguments.Add(Path.GetFullPath("dotnet-new3.dll"));
            Arguments.AddRange(args);
        }

        public DotnetNew3Command WithCustomHive(string? path = null)
        {
            path ??= TestUtils.CreateTemporaryFolder();
            Arguments.Add("--debug:custom-hive");
            Arguments.Add(path);
            _hiveSet = true;
            return this;
        }

        public DotnetNew3Command WithoutCustomHive()
        {
            _hiveSet = true;
            return this;
        }

        public DotnetNew3Command WithoutBuiltInTemplates()
        {
            Arguments.Add("--debug:disable-sdk-templates");
            return this;
        }

        protected override SdkCommandSpec CreateCommand(IEnumerable<string> args)
        {
            var sdkCommandSpec = new SdkCommandSpec()
            {
                FileName = "dotnet",
                Arguments = args.ToList(),
                WorkingDirectory = WorkingDirectory
            };

            if (!_hiveSet)
            {
                throw new Exception($"\"--debug:custom-hive\" is not set, call {nameof(WithCustomHive)} to set it or {nameof(WithoutCustomHive)} if it is intentional.");
            }

            return sdkCommandSpec;
        }
    }
}
