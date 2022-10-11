// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.NET.TestFramework.Commands;
using System.Collections.Generic;
using System.IO;
using System;
using Xunit.Abstractions;
using System.Linq;
using System.Net;

namespace Dotnet_new3.IntegrationTests
{
    public class DotnetNewCommand : TestCommand
    {
        private bool _hiveSet;

        public DotnetNewCommand(ITestOutputHelper log, params string[] args) : base(log)
        {
            // Set dotnet-new3.dll as first Argument to be passed to "dotnet"
            // And use full path since we want to execute in any working directory
            Arguments.Add(Path.GetFullPath("dotnet-new3.dll"));
            Arguments.AddRange(args);

            var dn3Path = System.Environment.GetEnvironmentVariable("DN3");
            if (string.IsNullOrEmpty(dn3Path))
            {
                var path = typeof(SharedHomeDirectory).Assembly.Location;
                while (path != null && !File.Exists(Path.Combine(path, "Microsoft.TemplateEngine.sln")))
                {
                    path = Path.GetDirectoryName(path);
                }
                dn3Path = path ?? throw new Exception("Couldn't find repository root, because \"Microsoft.TemplateEngine.sln\" is not in any of parent directories.");
            }

            Environment["DN3"] = dn3Path;

        }

        public DotnetNewCommand WithCustomHive(string path = null)
        {
            if (path == null)
            {
                path = Helpers.CreateTemporaryFolder();
            }
            Arguments.Add("--debug:custom-hive");
            Arguments.Add(path);
            _hiveSet = true;
            return this;
        }

        public DotnetNewCommand WithoutCustomHive()
        {
            _hiveSet = true;
            return this;
        }

        public DotnetNewCommand WithoutBuiltInTemplates()
        {
            Arguments.Add("--debug:disable-sdk-templates");
            return this;
        }

        public DotnetNewCommand WithDebug()
        {
            WithEnvironmentVariable("DOTNET_CLI_CONTEXT_VERBOSE", "true");
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
