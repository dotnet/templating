// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.PostActionProcessors;
using Microsoft.TemplateEngine.Mocks;
using Microsoft.TemplateEngine.TestHelper;
using Xunit;

namespace Microsoft.TemplateEngine.Cli.UnitTests
{
    public class AddReferencePostActionTests : IClassFixture<EnvironmentSettingsHelper>
    {
        private IEngineEnvironmentSettings _engineEnvironmentSettings;

        public AddReferencePostActionTests(EnvironmentSettingsHelper environmentSettingsHelper)
        {
            _engineEnvironmentSettings = environmentSettingsHelper.CreateEnvironment(hostIdentifier: this.GetType().Name, virtualize: true);
        }

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
            string targetBasePath = FileSystemHelpers.GetNewVirtualizedPath(_engineEnvironmentSettings);
            string projFileFullPath = Path.Combine(targetBasePath, "MyApp.proj");
            _engineEnvironmentSettings.Host.FileSystem.WriteAllText(projFileFullPath, TestCsprojFile);

            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();
            string outputBasePath = targetBasePath;

            IReadOnlyList<string> projFilesFound = actionProcessor.FindProjFileAtOrAbovePath(_engineEnvironmentSettings.Host.FileSystem, outputBasePath, new HashSet<string>());
            Assert.Equal(1, projFilesFound.Count);
        }

        [Fact(DisplayName = nameof(AddRefFindsOneNameConfiguredProjFileInOutputDirectory))]
        public void AddRefFindsOneNameConfiguredProjFileInOutputDirectory()
        {
            string targetBasePath = FileSystemHelpers.GetNewVirtualizedPath(_engineEnvironmentSettings);
            string fooprojFileFullPath = Path.Combine(targetBasePath, "MyApp.fooproj");
            _engineEnvironmentSettings.Host.FileSystem.WriteAllText(fooprojFileFullPath, TestCsprojFile);

            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();
            string outputBasePath = targetBasePath;

            HashSet<string> projectFileExtensions = new HashSet<string>() { ".fooproj" };
            IReadOnlyList<string> projFilesFound = actionProcessor.FindProjFileAtOrAbovePath(_engineEnvironmentSettings.Host.FileSystem, outputBasePath, projectFileExtensions);
            Assert.Equal(1, projFilesFound.Count);
        }

        [Fact(DisplayName = nameof(AddRefFindsOneNameConfiguredProjFileWhenMultipleExtensionsAreAllowed))]
        public void AddRefFindsOneNameConfiguredProjFileWhenMultipleExtensionsAreAllowed()
        {
            string targetBasePath = FileSystemHelpers.GetNewVirtualizedPath(_engineEnvironmentSettings);
            string fooprojFileFullPath = Path.Combine(targetBasePath, "MyApp.fooproj");
            _engineEnvironmentSettings.Host.FileSystem.WriteAllText(fooprojFileFullPath, TestCsprojFile);

            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();
            string outputBasePath = targetBasePath;

            HashSet<string> projectFileExtensions = new HashSet<string>() { ".fooproj", ".barproj" };
            IReadOnlyList<string> projFilesFound = actionProcessor.FindProjFileAtOrAbovePath(_engineEnvironmentSettings.Host.FileSystem, outputBasePath, projectFileExtensions);
            Assert.Equal(1, projFilesFound.Count);
        }

        [Fact(DisplayName = nameof(AddRefIgnoresOtherProjectTypesWhenMultipleTypesAreAllowed))]
        public void AddRefIgnoresOtherProjectTypesWhenMultipleTypesAreAllowed()
        {
            string targetBasePath = FileSystemHelpers.GetNewVirtualizedPath(_engineEnvironmentSettings);
            string fooprojFileFullPath = Path.Combine(targetBasePath, "MyApp.fooproj");
            _engineEnvironmentSettings.Host.FileSystem.WriteAllText(fooprojFileFullPath, TestCsprojFile);

            string barprojFileFullPath = Path.Combine(targetBasePath, "MyApp.barproj");
            _engineEnvironmentSettings.Host.FileSystem.WriteAllText(barprojFileFullPath, TestCsprojFile);

            string csprojFileFullPath = Path.Combine(targetBasePath, "MyApp.csproj");
            _engineEnvironmentSettings.Host.FileSystem.WriteAllText(csprojFileFullPath, TestCsprojFile);

            string fsprojFileFullPath = Path.Combine(targetBasePath, "MyApp.fsproj");
            _engineEnvironmentSettings.Host.FileSystem.WriteAllText(fsprojFileFullPath, TestCsprojFile);

            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();
            string outputBasePath = targetBasePath;

            HashSet<string> projectFileExtensions = new HashSet<string>() { ".bazproj", ".fsproj" };
            IReadOnlyList<string> projFilesFound = actionProcessor.FindProjFileAtOrAbovePath(_engineEnvironmentSettings.Host.FileSystem, outputBasePath, projectFileExtensions);
            Assert.Equal(1, projFilesFound.Count);
        }

        [Fact(DisplayName = nameof(AddRefFindsOneDefaultProjFileInAncestorOfOutputDirectory))]
        public void AddRefFindsOneDefaultProjFileInAncestorOfOutputDirectory()
        {
            string targetBasePath = FileSystemHelpers.GetNewVirtualizedPath(_engineEnvironmentSettings);
            string projFileFullPath = Path.Combine(targetBasePath, "MyApp.xproj");
            _engineEnvironmentSettings.Host.FileSystem.WriteAllText(projFileFullPath, TestCsprojFile);

            string outputBasePath = Path.Combine(targetBasePath, "ChildDir", "GrandchildDir");
            _engineEnvironmentSettings.Host.FileSystem.CreateDirectory(outputBasePath);

            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();
            IReadOnlyList<string> projFilesFound = actionProcessor.FindProjFileAtOrAbovePath(_engineEnvironmentSettings.Host.FileSystem, outputBasePath, new HashSet<string>());
            Assert.Equal(1, projFilesFound.Count);
        }

        [Fact(DisplayName = nameof(AddRefFindsMultipleDefaultProjFilesInOutputDirectory))]
        public void AddRefFindsMultipleDefaultProjFilesInOutputDirectory()
        {
            string projFilesOriginalContent = TestCsprojFile;
            string targetBasePath = FileSystemHelpers.GetNewVirtualizedPath(_engineEnvironmentSettings);
            string projFileFullPathOne = Path.Combine(targetBasePath, "MyApp.anysproj");
            _engineEnvironmentSettings.Host.FileSystem.WriteAllText(projFileFullPathOne, projFilesOriginalContent);

            string projFileFullPathTwo = Path.Combine(targetBasePath, "MyApp2.someproj");
            _engineEnvironmentSettings.Host.FileSystem.WriteAllText(projFileFullPathTwo, projFilesOriginalContent);

            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();
            string outputBasePath = targetBasePath;
            IReadOnlyList<string> projFilesFound = actionProcessor.FindProjFileAtOrAbovePath(_engineEnvironmentSettings.Host.FileSystem, outputBasePath, new HashSet<string>());
            Assert.Equal(2, projFilesFound.Count);
        }

        [Fact(DisplayName = nameof(AddRefFindsMultipleDefaultProjFilesInAncestorOfOutputDirectory))]
        public void AddRefFindsMultipleDefaultProjFilesInAncestorOfOutputDirectory()
        {
            string projFilesOriginalContent = TestCsprojFile;
            string targetBasePath = FileSystemHelpers.GetNewVirtualizedPath(_engineEnvironmentSettings);
            string projFileFullPathOne = Path.Combine(targetBasePath, "MyApp.fooproj");
            _engineEnvironmentSettings.Host.FileSystem.WriteAllText(projFileFullPathOne, projFilesOriginalContent);

            string projFileFullPathTwo = Path.Combine(targetBasePath, "MyApp2.barproj");
            _engineEnvironmentSettings.Host.FileSystem.WriteAllText(projFileFullPathTwo, projFilesOriginalContent);

            string outputBasePath = Path.Combine(targetBasePath, "ChildDir", "GrandchildDir");

            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();
            IReadOnlyList<string> projFilesFound = actionProcessor.FindProjFileAtOrAbovePath(_engineEnvironmentSettings.Host.FileSystem, outputBasePath, new HashSet<string>());
            Assert.Equal(2, projFilesFound.Count);
        }

        [Fact(DisplayName = nameof(AddRefCanTargetASingleProject))]
        public void AddRefCanTargetASingleProject()
        {
            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();

            string targetBasePath = _engineEnvironmentSettings.GetNewVirtualizedPath();
            string projFileFullPath = Path.Combine(targetBasePath, "MyApp.csproj");
            string referencedProjFileFullPath = Path.Combine(targetBasePath, "NewName.csproj");

            var args = new Dictionary<string, string>() { { "targetFiles", "MyApp.csproj" }, { "referenceType", "project" }, { "reference", "./NewName.csproj" } };
            var postAction = new MockPostAction { ActionId = AddReferencePostActionProcessor.ActionProcessorId, Args = args };

            var creationEffects = new MockCreationEffects()
                .WithFileChange(new MockFileChange("./NewName.csproj", "./NewName.csproj", ChangeKind.Create))
                .WithFileChange(new MockFileChange("./MyApp.csproj", "./MyApp.csproj", ChangeKind.Create));

            var callback = new MockAddProjectReferenceCallback();
            actionProcessor.Callbacks = new NewCommandCallbacks { AddProjectReference = callback.AddProjectReference };

            actionProcessor.Process(
                _engineEnvironmentSettings,
                postAction,
                creationEffects,
                new MockCreationResult(),
                targetBasePath);

            var call = callback.Calls.Single();

            Assert.Equal(projFileFullPath, call.Item1);
            Assert.Equal(new[] { referencedProjFileFullPath }, call.Item2);
        }

        [Fact(DisplayName = nameof(AddRefCanTargetASingleProject))]
        public void AddRefCanTargetMultipleProjects()
        {
            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();

            string targetBasePath = _engineEnvironmentSettings.GetNewVirtualizedPath();
            string projFile1FullPath = Path.Combine(targetBasePath, "MyApp.csproj");
            string projFile2FullPath = Path.Combine(targetBasePath, "AnotherApp.csproj");
            string referencedProjFileFullPath = Path.Combine(targetBasePath, "NewName.csproj");

            var args = new Dictionary<string, string>() { { "targetFiles", "MyApp.csproj;AnotherApp.csproj" }, { "referenceType", "project" }, { "reference", "./NewName.csproj" } };
            var postAction = new MockPostAction { ActionId = AddReferencePostActionProcessor.ActionProcessorId, Args = args };

            var creationEffects = new MockCreationEffects()
                .WithFileChange(new MockFileChange("./NewName.csproj", "./NewName.csproj", ChangeKind.Create))
                .WithFileChange(new MockFileChange("./AnotherApp.csproj", "./AnotherApp.csproj", ChangeKind.Create))
                .WithFileChange(new MockFileChange("./MyApp.csproj", "./MyApp.csproj", ChangeKind.Create));

            var callback = new MockAddProjectReferenceCallback();
            actionProcessor.Callbacks = new NewCommandCallbacks { AddProjectReference = callback.AddProjectReference };

            actionProcessor.Process(
                _engineEnvironmentSettings,
                postAction,
                creationEffects,
                new MockCreationResult(),
                targetBasePath);

            var call1 = callback.Calls.Single(x => x.Item1 == projFile1FullPath);
            var call2 = callback.Calls.Single(x => x.Item1 == projFile2FullPath);

            Assert.Equal(projFile1FullPath, call1.Item1);
            Assert.Equal(new[] { referencedProjFileFullPath }, call1.Item2);

            Assert.Equal(projFile2FullPath, call2.Item1);
            Assert.Equal(new[] { referencedProjFileFullPath }, call2.Item2);
        }

        [Fact(DisplayName = nameof(AddRefCanTargetASingleProject))]
        public void AddRefCanTargetMultipleProjectsWithAnArray()
        {
            AddReferencePostActionProcessor actionProcessor = new AddReferencePostActionProcessor();

            string targetBasePath = _engineEnvironmentSettings.GetNewVirtualizedPath();
            string projFile1FullPath = Path.Combine(targetBasePath, "MyApp.csproj");
            string projFile2FullPath = Path.Combine(targetBasePath, "AnotherApp.csproj");
            string referencedProjFileFullPath = Path.Combine(targetBasePath, "NewName.csproj");

            var args = new Dictionary<string, string>() { { "targetFiles", "[\"MyApp.csproj\", \"AnotherApp.csproj\"]" }, { "referenceType", "project" }, { "reference", "./NewName.csproj" } };
            var postAction = new MockPostAction { ActionId = AddReferencePostActionProcessor.ActionProcessorId, Args = args };

            var creationEffects = new MockCreationEffects()
                .WithFileChange(new MockFileChange("./NewName.csproj", "./NewName.csproj", ChangeKind.Create))
                .WithFileChange(new MockFileChange("./AnotherApp.csproj", "./AnotherApp.csproj", ChangeKind.Create))
                .WithFileChange(new MockFileChange("./MyApp.csproj", "./MyApp.csproj", ChangeKind.Create));

            var callback = new MockAddProjectReferenceCallback();
            actionProcessor.Callbacks = new NewCommandCallbacks { AddProjectReference = callback.AddProjectReference };

            actionProcessor.Process(
                _engineEnvironmentSettings,
                postAction,
                creationEffects,
                new MockCreationResult(),
                targetBasePath);

            var call1 = callback.Calls.Single(x => x.Item1 == projFile1FullPath);
            var call2 = callback.Calls.Single(x => x.Item1 == projFile2FullPath);

            Assert.Equal(projFile1FullPath, call1.Item1);
            Assert.Equal(new[] { referencedProjFileFullPath }, call1.Item2);

            Assert.Equal(projFile2FullPath, call2.Item1);
            Assert.Equal(new[] { referencedProjFileFullPath }, call2.Item2);
        }

        private class MockAddProjectReferenceCallback
        {
            public List<(string, IReadOnlyList<string>)> Calls { get; } = new();

            public bool AddProjectReference(string target, IReadOnlyList<string> references)
            {
                Calls.Add((target, references));

                return true;
            }
        }
    }
}
