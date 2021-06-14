// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.NET.TestFramework.Commands;
using Microsoft.NET.TestFramework.Assertions;
using Microsoft.TemplateEngine.TestHelper;

namespace Microsoft.TemplateSearch.TemplateDiscovery.Test
{
    internal static class CacheFileTests
    {
        public static void RunTests()
        {
            CanSearchWhileInstantiating("3.1");
            CanCheckUpdates("3.1");
            CanUpdate("3.1");

            CanSearchWhileInstantiating("5.0.300");
            CanCheckUpdates("5.0.300");
            CanUpdate("5.0.300");
            CanSearch("5.0.300");

            CanSearch("6.0");
        }

        private static void CanSearchWhileInstantiating(string sdkVersion)
        {
            string workingDirectory = TestUtils.CreateTemporaryFolder();
            CreateGlobalJson(workingDirectory, sdkVersion);

            new DotnetCommand(null, "--version")
                .WithWorkingDirectory(workingDirectory)
                .Execute()
                .Should()
                .ExitWith(0)
                .And
                .NotHaveStdErr()
                .And.HaveStdOutContaining($"Version   {sdkVersion}");
        }

        private static void CanCheckUpdates(string sdkVersion)
        {

        }

        private static void CanUpdate(string sdkVersion)
        {

        }

        private static void CanSearch(string sdkVersion)
        {

        }

        private static void CreateGlobalJson(string directory, string sdkVersion)
        {
            string jsonContent = $@"{{ ""sdk"": {{ ""version"": ""{sdkVersion}"" }} }}";
            File.WriteAllText(Path.Combine(directory, "global.json"), jsonContent);
        }
    }
}
