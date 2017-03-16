﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Mocks;
using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.UnitTests.TemplateConfigTests
{
    public class TemplateConfigTestHelpers
    {
        public static readonly Guid FileSystemMountPointFactoryId = new Guid("8C19221B-DEA3-4250-86FE-2D4E189A11D2");
        public static readonly string DefaultConfigRelativePath = ".template.config/template.json";

        public static IEngineEnvironmentSettings GetTestEnvironment()
        {
            ITemplateEngineHost host = new TestHost
            {
                HostIdentifier = "TestRunner",
                Version = "1.0.0.0",
                Locale = "en-US"
            };

            return new EngineEnvironmentSettings(host, x => null);
        }

        // Note: this does not deal with configs split into multiple files.
        public static IRunnableProjectConfig ConfigFromSource(IEngineEnvironmentSettings environment, IMountPoint mountPoint, string configFile = null)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                configFile = DefaultConfigRelativePath;
            }

            string fullPath = Path.Combine(mountPoint.Info.Place, configFile);
            string configContent = environment.Host.FileSystem.ReadAllText(fullPath);

            JObject configJson = JObject.Parse(configContent);
            return SimpleConfigModel.FromJObject(environment, configJson);
        }

        public static IFileSystemInfo ConfigFileSystemInfo(IMountPoint mountPoint, string configFile = null)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                configFile = DefaultConfigRelativePath;
            }

            return mountPoint.FileInfo(configFile);
        }

        public static string GetNewVirtualizedPath(IEngineEnvironmentSettings environment)
        {
            string basePath = Directory.GetCurrentDirectory() + "/sandbox/" + Guid.NewGuid() + "/";
            environment.Host.VirtualizeDirectory(basePath);

            return basePath;
        }

        public static void SetupFileSourceMatchersOnGlobalRunSpec(MockGlobalRunSpec runSpec, FileSourceMatchInfo source)
        {
            FileSourceHierarchicalPathMatcher matcher = new FileSourceHierarchicalPathMatcher(source);
            runSpec.Include = new List<IPathMatcher>() { new FileSourceStateMatcher(FileDispositionStates.Include, matcher) };
            runSpec.Exclude = new List<IPathMatcher>() { new FileSourceStateMatcher(FileDispositionStates.Exclude, matcher) };
            runSpec.CopyOnly = new List<IPathMatcher>() { new FileSourceStateMatcher(FileDispositionStates.CopyOnly, matcher) };
        }
    }
}
