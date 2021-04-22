// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TemplateEngine.TemplateLocalizer.EndToEndTests
{
    [TestClass]
    public class ExportCommandTests
    {
        private const string ComplexTemplateJson = @"{
    ""$schema"": ""http://json.schemastore.org/template"",
    ""author"": ""Microsoft"",
    ""classifications"": [""Common"", ""Library""],
    ""name"": ""Class library"",
    ""generatorVersions"": ""[1.0.0.0-*)"",
    ""description"": ""dEscRiPtiON: ,./|\\<>{}!@#$%^&*()_+-=? 12 äÄßöÖüÜçÇğĞıIİşŞ"",
    ""symbols"": {
        ""TargetFrameworkOverride"": {
          ""type"": ""parameter"",
          ""displayName"": ""tfm display name"",
          ""description"": ""tfm description"",
          ""replaces"": ""TargetFrameworkOverride"",
          ""datatype"": ""string"",
          ""defaultValue"": """"
        },
        ""Framework"": {
          ""type"": ""parameter"",
          ""displayName"": ""framework display name"",
          ""description"": ""framework description"",
          ""datatype"": ""choice"",
          ""choices"": [
            {
              ""choice"": ""net5.0"",
              ""displayName"": ""net5.0 display name"",
              ""description"": ""Target net5.0""
            },
            {
              ""choice"": ""netstandard2.1"",
              ""description"": ""Target netstandard2.1""
            },
            {
              ""choice"": ""netstandard2.0"",
              ""displayName"": ""netstandard2.0 display name"",
              ""description"": ""Target netstandard2.0""
            }
          ],
          ""replaces"": ""net5.0"",
          ""defaultValue"": ""net5.0""
        },
    },
    ""postActions"": [
        {
          ""condition"": ""(!skipRestore)"",
          ""description"": ""Restore NuGet packages required by this project."",
          ""manualInstructions"": [
            {
                ""text"": ""Run 'dotnet restore'""
            }
          ],
          ""actionId"": ""210D431B-A78B-4D2F-B762-4ED3E3EA9025"",
          ""continueOnError"": true
        },
        {
        ""condition"": ""(HostIdentifier != \""dotnetcli\"" && HostIdentifier != \""dotnetcli-preview\"")"",
          ""description"": ""Opens Class1.cs in the editor"",
          ""manualInstructions"": [ ],
          ""actionId"": ""84C0DA21-51C8-4541-9940-6CA19AF04EE6"",
          ""args"": {
            ""files"": ""1""
          },
          ""continueOnError"": true
        }
      ]
}
";

        private string _workingDirectory;

        [TestInitialize]
        public void Initialize()
        {
            _workingDirectory = Path.Combine(Path.GetTempPath(), "Microsoft.TemplateEngine.TemplateLocalizer.EndToEndTests", Path.GetRandomFileName());
            Directory.CreateDirectory(_workingDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(_workingDirectory, true);
        }

        [TestMethod]
        public async Task LocFilesAreExported()
        {
            string[] exportedFiles = await CreateAndExportTemplateJson(
                ComplexTemplateJson,
                _workingDirectory,
                args: new string[] { "export", _workingDirectory })
                .ConfigureAwait(false);

            Assert.IsTrue(exportedFiles.Length > 0);
            Assert.IsTrue(exportedFiles.All(p => p.EndsWith(".templatestrings.json")));
        }

        [TestMethod]
        public async Task LocFilesAreNotExportedWithDryRun()
        {
            string[] exportedFiles = await CreateAndExportTemplateJson(
                ComplexTemplateJson,
                _workingDirectory,
                args: new string[] { "export", _workingDirectory, "--dry-run" })
                .ConfigureAwait(false);

            Assert.IsTrue(exportedFiles.Length == 0);
        }

        [TestMethod]
        public async Task LanguagesCanBeOverriden()
        {
            string[] exportedFiles = await CreateAndExportTemplateJson(
                ComplexTemplateJson,
                _workingDirectory,
                args: new string[] { "export", _workingDirectory, "--language", "tr" })
                .ConfigureAwait(false);

            Assert.IsTrue(exportedFiles.Length == 1);
            Assert.IsTrue(File.Exists(Path.Combine(_workingDirectory, "localize", "tr.templatestrings.json")));
            Assert.IsFalse(File.Exists(Path.Combine(_workingDirectory, "localize", "es.templatestrings.json")));
        }

        [TestMethod]
        public async Task SubdirectoriesAreNotSearchedByDefault()
        {
            Directory.CreateDirectory(Path.Combine(_workingDirectory, "subdir"));
            Directory.CreateDirectory(Path.Combine(_workingDirectory, "subdir2"));

            await File.WriteAllTextAsync(Path.Combine(_workingDirectory, "subdir", "template.json"), ComplexTemplateJson).ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(_workingDirectory, "subdir2", "template.json"), ComplexTemplateJson).ConfigureAwait(false);

            int runResult = await Program.Main(new string[] { "export", _workingDirectory, "--language", "es" }).ConfigureAwait(false);
            // Error: no templates found under the given folder.
            Assert.IsTrue(runResult != 0);

            Assert.IsFalse(File.Exists(Path.Combine(_workingDirectory, "subdir", "localize", "es.templatestrings.json")));
            Assert.IsFalse(File.Exists(Path.Combine(_workingDirectory, "subdir2", "localize", "es.templatestrings.json")));
        }

        [TestMethod]
        public async Task SubdirectoriesCanBeSearched()
        {
            Directory.CreateDirectory(Path.Combine(_workingDirectory, "subdir", ".template.config"));
            Directory.CreateDirectory(Path.Combine(_workingDirectory, ".template.config"));

            await File.WriteAllTextAsync(Path.Combine(_workingDirectory, "subdir", ".template.config", "template.json"), ComplexTemplateJson).ConfigureAwait(false);
            await File.WriteAllTextAsync(Path.Combine(_workingDirectory, ".template.config", "template.json"), ComplexTemplateJson).ConfigureAwait(false);

            int runResult = await Program.Main(new string[] { "export", _workingDirectory, "--language", "es", "--recursive" }).ConfigureAwait(false);
            Assert.IsTrue(runResult == 0);

            Assert.IsTrue(File.Exists(Path.Combine(_workingDirectory, "subdir", ".template.config", "localize", "es.templatestrings.json")));
            Assert.IsTrue(File.Exists(Path.Combine(_workingDirectory, ".template.config", "localize", "es.templatestrings.json")));
        }

        [TestMethod]
        public async Task SubdirectoriesWithoutTemplateConfigFileAreNotSearched()
        {
            Directory.CreateDirectory(Path.Combine(_workingDirectory, "subdir"));

            await File.WriteAllTextAsync(Path.Combine(_workingDirectory, "subdir", "template.json"), ComplexTemplateJson).ConfigureAwait(false);

            int runResult = await Program.Main(new string[] { "export", _workingDirectory, "--language", "es", "--recursive" }).ConfigureAwait(false);
            // Error: no templates found under the given folder.
            Assert.IsTrue(runResult != 0);

            Assert.IsFalse(File.Exists(Path.Combine(_workingDirectory, "subdir", "localize", "es.templatestrings.json")));
        }

        /// <summary>
        /// Creates a template.json file with given content in the given directory.
        /// Runs the template localizer tool with given arguments.
        /// Returns all the files found under "localize" folder.
        /// </summary>
        private static async Task<string[]> CreateAndExportTemplateJson(string jsonContent, string directory, params string[] args)
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "template.json"), jsonContent).ConfigureAwait(false);

            int runResult = await Program.Main(args).ConfigureAwait(false);
            Assert.IsTrue(runResult == 0);

            string expectedExportDirectory = Path.Combine(directory, "localize");
            try
            {
                return Directory.GetFiles(expectedExportDirectory);
            }
            catch (DirectoryNotFoundException)
            {
                // Since no templates were created, it is normal that no directory was created.
                return Array.Empty<string>();
            }
        }
    }
}
