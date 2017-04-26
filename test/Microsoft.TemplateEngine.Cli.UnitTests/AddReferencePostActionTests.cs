using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.TemplateEngine.Cli.PostActionProcessors;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests
{
    public class AddReferencePostActionTests : TestBase
    {
        private static string TestCsprojFile
        {
            get
            {
                return @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp1.1</TargetFramework>
  </PropertyGroup>
</Project>";
            }
        }

        [Fact(DisplayName = nameof(AddRefFindsOneDefaultProjFileInOutputDirectory))]
        public void AddRefFindsOneDefaultProjFileInOutputDirectory()
        {
            string targetBasePath = FileSystemHelpers.GetNewVirtualizedPath(EnvironmentSettings);
            string projFileFullPath = Path.Combine(targetBasePath, "MyApp.proj");
            EnvironmentSettings.Host.FileSystem.WriteAllText(projFileFullPath, TestCsprojFile);

            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();
            string outputBasePath = targetBasePath;

            IReadOnlyList<string> projFilesFound = actionProcessor.FindProjFileAtOrAbovePath(EnvironmentSettings.Host.FileSystem, outputBasePath, new HashSet<string>());
            Assert.Equal(1, projFilesFound.Count);
        }

        [Fact(DisplayName = nameof(AddRefFindsOneNameConfiguredProjFileInOutputDirectory))]
        public void AddRefFindsOneNameConfiguredProjFileInOutputDirectory()
        {
            string targetBasePath = FileSystemHelpers.GetNewVirtualizedPath(EnvironmentSettings);
            string fooprojFileFullPath = Path.Combine(targetBasePath, "MyApp.fooproj");
            EnvironmentSettings.Host.FileSystem.WriteAllText(fooprojFileFullPath, TestCsprojFile);

            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();
            string outputBasePath = targetBasePath;

            HashSet<string> projectFileExtensions = new HashSet<string>() { ".fooproj" };
            IReadOnlyList<string> projFilesFound = actionProcessor.FindProjFileAtOrAbovePath(EnvironmentSettings.Host.FileSystem, outputBasePath, projectFileExtensions);
            Assert.Equal(1, projFilesFound.Count);
        }

        [Fact(DisplayName = nameof(AddRefFindsOneNameConfiguredProjFileWhenMultipleExtensionsAreAllowed))]
        public void AddRefFindsOneNameConfiguredProjFileWhenMultipleExtensionsAreAllowed()
        {
            string targetBasePath = FileSystemHelpers.GetNewVirtualizedPath(EnvironmentSettings);
            string fooprojFileFullPath = Path.Combine(targetBasePath, "MyApp.fooproj");
            EnvironmentSettings.Host.FileSystem.WriteAllText(fooprojFileFullPath, TestCsprojFile);

            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();
            string outputBasePath = targetBasePath;

            HashSet<string> projectFileExtensions = new HashSet<string>() { ".fooproj", ".barproj" };
            IReadOnlyList<string> projFilesFound = actionProcessor.FindProjFileAtOrAbovePath(EnvironmentSettings.Host.FileSystem, outputBasePath, projectFileExtensions);
            Assert.Equal(1, projFilesFound.Count);
        }

        [Fact(DisplayName = nameof(AddRefIgnoresOtherProjectTypesWhenMultipleTypesAreAllowed))]
        public void AddRefIgnoresOtherProjectTypesWhenMultipleTypesAreAllowed()
        {
            string targetBasePath = FileSystemHelpers.GetNewVirtualizedPath(EnvironmentSettings);
            string fooprojFileFullPath = Path.Combine(targetBasePath, "MyApp.fooproj");
            EnvironmentSettings.Host.FileSystem.WriteAllText(fooprojFileFullPath, TestCsprojFile);

            string barprojFileFullPath = Path.Combine(targetBasePath, "MyApp.barproj");
            EnvironmentSettings.Host.FileSystem.WriteAllText(barprojFileFullPath, TestCsprojFile);

            string csprojFileFullPath = Path.Combine(targetBasePath, "MyApp.csproj");
            EnvironmentSettings.Host.FileSystem.WriteAllText(csprojFileFullPath, TestCsprojFile);

            string fsprojFileFullPath = Path.Combine(targetBasePath, "MyApp.fsproj");
            EnvironmentSettings.Host.FileSystem.WriteAllText(fsprojFileFullPath, TestCsprojFile);

            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();
            string outputBasePath = targetBasePath;

            HashSet<string> projectFileExtensions = new HashSet<string>() { ".bazproj", ".fsproj" };
            IReadOnlyList<string> projFilesFound = actionProcessor.FindProjFileAtOrAbovePath(EnvironmentSettings.Host.FileSystem, outputBasePath, projectFileExtensions);
            Assert.Equal(1, projFilesFound.Count);
        }

        [Fact(DisplayName = nameof(AddRefFindsOneDefaultProjFileInAncestorOfOutputDirectory))]
        public void AddRefFindsOneDefaultProjFileInAncestorOfOutputDirectory()
        {
            string targetBasePath = FileSystemHelpers.GetNewVirtualizedPath(EnvironmentSettings);
            string projFileFullPath = Path.Combine(targetBasePath, "MyApp.xproj");
            EnvironmentSettings.Host.FileSystem.WriteAllText(projFileFullPath, TestCsprojFile);

            string outputBasePath = Path.Combine(targetBasePath, "ChildDir", "GrandchildDir");
            EnvironmentSettings.Host.FileSystem.CreateDirectory(outputBasePath);

            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();
            IReadOnlyList<string> projFilesFound = actionProcessor.FindProjFileAtOrAbovePath(EnvironmentSettings.Host.FileSystem, outputBasePath, new HashSet<string>());
            Assert.Equal(1, projFilesFound.Count);
        }

        [Fact(DisplayName = nameof(AddRefFindsMultipleDefaultProjFilesInOutputDirectory))]
        public void AddRefFindsMultipleDefaultProjFilesInOutputDirectory()
        {
            string projFilesOriginalContent = TestCsprojFile;
            string targetBasePath = FileSystemHelpers.GetNewVirtualizedPath(EnvironmentSettings);
            string projFileFullPathOne = Path.Combine(targetBasePath, "MyApp.anysproj");
            EnvironmentSettings.Host.FileSystem.WriteAllText(projFileFullPathOne, projFilesOriginalContent);

            string projFileFullPathTwo = Path.Combine(targetBasePath, "MyApp2.someproj");
            EnvironmentSettings.Host.FileSystem.WriteAllText(projFileFullPathTwo, projFilesOriginalContent);

            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();
            string outputBasePath = targetBasePath;
            IReadOnlyList<string> projFilesFound = actionProcessor.FindProjFileAtOrAbovePath(EnvironmentSettings.Host.FileSystem, outputBasePath, new HashSet<string>());
            Assert.Equal(2, projFilesFound.Count);
        }

        [Fact(DisplayName = nameof(AddRefFindsMultipleDefaultProjFilesInAncestorOfOutputDirectory))]
        public void AddRefFindsMultipleDefaultProjFilesInAncestorOfOutputDirectory()
        {
            string projFilesOriginalContent = TestCsprojFile;
            string targetBasePath = FileSystemHelpers.GetNewVirtualizedPath(EnvironmentSettings);
            string projFileFullPathOne = Path.Combine(targetBasePath, "MyApp.fooproj");
            EnvironmentSettings.Host.FileSystem.WriteAllText(projFileFullPathOne, projFilesOriginalContent);

            string projFileFullPathTwo = Path.Combine(targetBasePath, "MyApp2.barproj");
            EnvironmentSettings.Host.FileSystem.WriteAllText(projFileFullPathTwo, projFilesOriginalContent);

            string outputBasePath = Path.Combine(targetBasePath, "ChildDir", "GrandchildDir");

            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();
            IReadOnlyList<string> projFilesFound = actionProcessor.FindProjFileAtOrAbovePath(EnvironmentSettings.Host.FileSystem, outputBasePath, new HashSet<string>());
            Assert.Equal(2, projFilesFound.Count);
        }
    }
}
