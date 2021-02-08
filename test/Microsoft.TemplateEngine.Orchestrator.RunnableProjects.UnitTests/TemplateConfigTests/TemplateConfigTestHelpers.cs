using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Mocks;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Config;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros;
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
            TestHost host = new TestHost
            {
                HostIdentifier = "TestRunner",
                Version = "1.0.0.0",
                Locale = "en-US"
            };

            host.FileSystem = new PhysicalFileSystem();
            return new EngineEnvironmentSettings(host, x => null);
        }

        // Note: this does not deal with configs split into multiple files.
        public static IRunnableProjectConfig ConfigFromSource(IEngineEnvironmentSettings environment, IMountPoint mountPoint, string configFile = null)
        {
            if (string.IsNullOrEmpty(configFile))
            {
                configFile = DefaultConfigRelativePath;
            }

            using Stream stream = mountPoint.FileInfo(configFile).OpenRead();
            using StreamReader streamReader = new StreamReader(stream);
            string configContent = streamReader.ReadToEnd();

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

        public static void SetupFileSourceMatchersOnGlobalRunSpec(MockGlobalRunSpec runSpec, FileSourceMatchInfo source)
        {
            FileSourceHierarchicalPathMatcher matcher = new FileSourceHierarchicalPathMatcher(source);
            runSpec.Include = new List<IPathMatcher>() { new FileSourceStateMatcher(FileDispositionStates.Include, matcher) };
            runSpec.Exclude = new List<IPathMatcher>() { new FileSourceStateMatcher(FileDispositionStates.Exclude, matcher) };
            runSpec.CopyOnly = new List<IPathMatcher>() { new FileSourceStateMatcher(FileDispositionStates.CopyOnly, matcher) };
            runSpec.Rename = source.Renames ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public static IComponentManager SetupMockComponentManager()
        {
            MockComponentManager components = new MockComponentManager();

            components.Register(typeof(CaseChangeMacro));
            components.Register(typeof(ConstantMacro));
            components.Register(typeof(EvaluateMacro));
            components.Register(typeof(GuidMacro));
            components.Register(typeof(NowMacro));
            components.Register(typeof(RandomMacro));
            components.Register(typeof(RegexMacro));
            components.Register(typeof(SwitchMacro));
            components.Register(typeof(BalancedNestingConfig));
            components.Register(typeof(ConditionalConfig));
            components.Register(typeof(FlagsConfig));
            components.Register(typeof(IncludeConfig));
            components.Register(typeof(RegionConfig));
            components.Register(typeof(ReplacementConfig));

            // would need the CLI project (or other implementer)
            //components.Register(typeof(DotnetRestorePostActionProcessor));
            //components.Register(typeof(InstructionDisplayPostActionProcessor));

            return components;
        }
    }
}
