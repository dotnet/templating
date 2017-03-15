﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Xunit;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.UnitTests.TemplateConfigTests
{
    public class SourceConfigTests
    {
        [Fact(DisplayName = nameof(SourceConfigExcludesAreOverriddenByIncludes))]
        public void SourceConfigExcludesAreOverriddenByIncludes()
        {
            IEngineEnvironmentSettings environment = TemplateConfigTestHelpers.GetTestEnvironment();
            string sourceBasePath = TemplateConfigTestHelpers.GetNewVirtualizedPath(environment);

            TestTemplateSetup setup = SetupTwoFilesWithConfigExtensionTemplate(environment, sourceBasePath);
            string targetDir = TemplateConfigTestHelpers.GetNewVirtualizedPath(environment);
            setup.InstantiateTemplate(targetDir);

            Assert.True(environment.Host.FileSystem.FileExists(Path.Combine(targetDir, "core.config")));
            Assert.False(environment.Host.FileSystem.FileExists(Path.Combine(targetDir, "full.config")));
        }

        [Fact(DisplayName = nameof(CopyOnlyWithoutIncludeDoesntHappen))]
        public void CopyOnlyWithoutIncludeDoesntHappen()
        {
            IEngineEnvironmentSettings environment = TemplateConfigTestHelpers.GetTestEnvironment();
            string sourceBasePath = TemplateConfigTestHelpers.GetNewVirtualizedPath(environment);

            TestTemplateSetup setup = SetupCopyOnlyTemplate(environment, sourceBasePath);
            string targetDir = TemplateConfigTestHelpers.GetNewVirtualizedPath(environment);
            IReadOnlyDictionary<string, IReadOnlyList<IFileChange>> allChanges = setup.GetFileChanges(targetDir);

            // one source, should cause one set of changes
            Assert.Equal(1, allChanges.Count);

            if (!allChanges.TryGetValue("./", out IReadOnlyList<IFileChange> changes))
            {
                Assert.True(false, "no changes for source './'");
            }

            Assert.Equal(1, changes.Count);
            Assert.Equal(ChangeKind.Create, changes[0].ChangeKind);
            Assert.True(string.Equals(changes[0].TargetRelativePath, "something.txt"), "didn't copy the correct file");
        }

        [Fact(DisplayName = nameof(IncludeModifierOverridesPreviousExcludeModifierTemplateTest))]
        public void IncludeModifierOverridesPreviousExcludeModifierTemplateTest()
        {
            IEngineEnvironmentSettings environment = TemplateConfigTestHelpers.GetTestEnvironment();
            string sourceBasePath = TemplateConfigTestHelpers.GetNewVirtualizedPath(environment);

            TestTemplateSetup setup = SetupXYZFilesForModifierOverrideTestsTemplate(environment, sourceBasePath, IncludeModifierOverridesPreviousExcludeModifierConfigText);
            string targetDir = TemplateConfigTestHelpers.GetNewVirtualizedPath(environment);
            IReadOnlyDictionary<string, IReadOnlyList<IFileChange>> allChanges = setup.GetFileChanges(targetDir);

            Assert.Equal(1, allChanges.Count);

            if (!allChanges.TryGetValue("./", out IReadOnlyList<IFileChange> changes))
            {
                Assert.True(false, "no changes for source './'");
            }

            Assert.Equal(1, changes.Count);
            Assert.Equal(ChangeKind.Create, changes[0].ChangeKind);
            Assert.True(string.Equals(changes[0].TargetRelativePath, "include.xyz"), "include modifier didn't properly override exclude modifier");
        }

        [Fact(DisplayName = nameof(ExcludeModifierOverridesPreviousIncludeModifierTemplateTest))]
        public void ExcludeModifierOverridesPreviousIncludeModifierTemplateTest()
        {
            IEngineEnvironmentSettings environment = TemplateConfigTestHelpers.GetTestEnvironment();
            string sourceBasePath = TemplateConfigTestHelpers.GetNewVirtualizedPath(environment);

            TestTemplateSetup setup = SetupXYZFilesForModifierOverrideTestsTemplate(environment, sourceBasePath, ExcludeModifierOverridesPreviousIncludeModifierConfigText);
            string targetDir = TemplateConfigTestHelpers.GetNewVirtualizedPath(environment);
            IReadOnlyDictionary<string, IReadOnlyList<IFileChange>> allChanges = setup.GetFileChanges(targetDir);

            Assert.Equal(1, allChanges.Count);

            if (!allChanges.TryGetValue("./", out IReadOnlyList<IFileChange> changes))
            {
                Assert.True(false, "no changes for source './'");
            }

            Assert.Equal(2, changes.Count);

            IFileChange includeXyzChangeInfo = changes.FirstOrDefault(x => string.Equals(x.TargetRelativePath, "include.xyz"));
            Assert.NotNull(includeXyzChangeInfo);
            Assert.Equal(ChangeKind.Create, includeXyzChangeInfo.ChangeKind);

            IFileChange otherXyzChangeInfo = changes.FirstOrDefault(x => string.Equals(x.TargetRelativePath, "other.xyz"));
            Assert.NotNull(otherXyzChangeInfo);
            Assert.Equal(ChangeKind.Create, otherXyzChangeInfo.ChangeKind);
        }

        private static TestTemplateSetup SetupTwoFilesWithConfigExtensionTemplate(IEngineEnvironmentSettings environment, string basePath)
        {
            IDictionary<string, string> templateSourceFiles = new Dictionary<string, string>();
            // config
            templateSourceFiles.Add(TemplateConfigTestHelpers.DefaultConfigRelativePath, ExcludesWithIncludeModifierConfigText);
            // content
            templateSourceFiles.Add("core.config", null);
            templateSourceFiles.Add("full.config", null);
            TestTemplateSetup setup = new TestTemplateSetup(environment, basePath, templateSourceFiles);
            setup.WriteSource();
            return setup;
        }

        private static string ExcludesWithIncludeModifierConfigText
        {
            get
            {
                string configString = @"
{
  ""sources"": [
    {
      ""exclude"": [ ""**/*.config"" ],
      ""modifiers"": [
        {
          ""include"": [""core.config""]
        }
      ]
    }
  ]
}";
                return configString;
            }
        }

        private static TestTemplateSetup SetupCopyOnlyTemplate(IEngineEnvironmentSettings environment, string basePath)
        {
            IDictionary<string, string> templateSourceFiles = new Dictionary<string, string>();
            templateSourceFiles.Add(TemplateConfigTestHelpers.DefaultConfigRelativePath, CopyOnlyWithoutAnIncludeConfigText);
            templateSourceFiles.Add("something.txt", null);
            templateSourceFiles.Add("copy.me", null);
            TestTemplateSetup setup = new TestTemplateSetup(environment, basePath, templateSourceFiles);
            setup.WriteSource();
            return setup;
        }

        private static string CopyOnlyWithoutAnIncludeConfigText
        {
            get
            {
                string configString = @"
{
  ""sources"": [
    {
      ""include"": [ ""**/*.txt"" ],
      ""modifiers"": [
        {
          ""copyOnly"": [""copy.me""]
        },
      ]
    }
  ]
}";
                return configString;
            }
        }

        private static TestTemplateSetup SetupXYZFilesForModifierOverrideTestsTemplate(IEngineEnvironmentSettings environment, string basePath, string templateConfig)
        {
            IDictionary<string, string> templateSourceFiles = new Dictionary<string, string>();
            templateSourceFiles.Add(TemplateConfigTestHelpers.DefaultConfigRelativePath, templateConfig);
            templateSourceFiles.Add("other.xyz", null);
            templateSourceFiles.Add("include.xyz", null);
            templateSourceFiles.Add("exclude.xyz", null);
            TestTemplateSetup setup = new TestTemplateSetup(environment, basePath, templateSourceFiles);
            setup.WriteSource();
            return setup;
        }

        private static string IncludeModifierOverridesPreviousExcludeModifierConfigText
        {
            get
            {
                string configString = @"
{
  ""sources"": [
    {
      // use the default ICE
      ""modifiers"": [
        {
          ""exclude"": [""*.xyz""]
        },
        {
          ""include"": [""include.xyz""]
        }
      ]
    }
  ]
}
";
                return configString;
            }
        }

        private static string ExcludeModifierOverridesPreviousIncludeModifierConfigText
        {
            get
            {
                string configString = @"
{
  ""sources"": [
    {
      // use the default ICE
      ""modifiers"": [
        {
          ""include"": [""*.xyz""]
        },
        {
          ""exclude"": [""exclude.xyz""]
        }
      ]
    }
  ]
}
";
                return configString;
            }
        }
    }
}
