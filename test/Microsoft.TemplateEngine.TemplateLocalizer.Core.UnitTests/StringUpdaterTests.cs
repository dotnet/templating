// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Core.UnitTests
{
    [TestClass]
    public class StringUpdaterTests
    {
        private static readonly IReadOnlyList<TemplateString> InputStrings = new List<TemplateString>()
        {
            new ("..name", "name", "Class library"),
            new ("..description", "description", "dEscRiPtiON: ,./|\\<>{}!@#$%^&*()_+-=? 12 äÄßöÖüÜçÇğĞıIİşŞ"),
            new ("..symbols.targetframeworkoverride.displayname", "symbols.TargetFrameworkOverride.displayName", "tfm display name"),
            new ("..symbols.targetframeworkoverride.description", "symbols.TargetFrameworkOverride.description", "tfm description"),
            new ("..symbols.framework.displayname", "symbols.Framework.displayName", "framework display name"),
            new ("..symbols.framework.description", "symbols.Framework.description", "framework description"),
            new ("..symbols.framework.choices.0.displayname", "symbols.Framework.choices.net5_0.displayName", "net5.0 display name"),
            new ("..symbols.framework.choices.0.description", "symbols.Framework.choices.net5_0.description", "Target net5.0"),
            new ("..symbols.framework.choices.1.description", "symbols.Framework.choices.netstandard2_1.description", "Target netstandard2.1"),
            new ("..symbols.framework.choices.2.displayname", "symbols.Framework.choices.netstandard2_0.displayName", "netstandard2.0 display name"),
            new ("..symbols.framework.choices.2.description", "symbols.Framework.choices.netstandard2_0.description", "Target netstandard2.0"),
            new ("..postactions.0.description", "postActions[0].description", "Restore NuGet packages required by this project."),
            new ("..postactions.0.manualinstructions.0.text", "postActions[0].manualInstructions[0].text", "Run 'dotnet restore'"),
            new ("..postactions.1.description", "postActions[1].description", "Opens Class1.cs in the editor")
        };

        private string _workingDirectory;

        [TestInitialize]
        public void Initialize()
        {
            _workingDirectory = Path.Combine(Path.GetTempPath(), "Microsoft.TemplateEngine.TemplateLocalizer.Core.UnitTests", Path.GetRandomFileName());
            Directory.CreateDirectory(_workingDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(_workingDirectory, true);
        }

        [TestMethod]
        public async Task AllStringsAreWrittenToFile()
        {
            CancellationTokenSource cts = new CancellationTokenSource(10000);
            await TemplateStringUpdater.UpdateStringsAsync(InputStrings, "en", new string[] { "tr" }, _workingDirectory, dryRun: false, NullLogger.Instance, cts.Token)
                .ConfigureAwait(false);

            string expectedFilename = Path.Combine(_workingDirectory, "tr.templatestrings.json");
            Assert.IsTrue(File.Exists(expectedFilename));

            // Only the specified language file should be generated (no more than 1 file in the directory).
            Assert.IsTrue(Directory.EnumerateFileSystemEntries(_workingDirectory).Count() == 1);

            Dictionary<string, string> resultStrings = await ReadTemplateStringsFromJsonFile(expectedFilename, cts.Token).ConfigureAwait(false);

            // All the InputStrings should be in the resultStrings
            Assert.IsTrue(InputStrings.All(i => resultStrings.TryGetValue(i.LocalizationKey, out var value) && value == i.Value));
        }

        [TestMethod]
        public async Task DryRunPreventsWritingToFileSystem()
        {
            CancellationTokenSource cts = new CancellationTokenSource(10000);
            await TemplateStringUpdater.UpdateStringsAsync(InputStrings, "en", new string[] { "tr" }, _workingDirectory, dryRun: true, NullLogger.Instance, cts.Token)
                .ConfigureAwait(false);

            string expectedFilename = Path.Combine(_workingDirectory, "tr.templatestrings.json");
            Assert.IsFalse(File.Exists(expectedFilename));
        }

        [TestMethod]
        public async Task StringOrderIsPreserved()
        {
            CancellationTokenSource cts = new CancellationTokenSource(10000);
            await TemplateStringUpdater.UpdateStringsAsync(InputStrings, "en", new string[] { "tr" }, _workingDirectory, dryRun: false, NullLogger.Instance, cts.Token)
                .ConfigureAwait(false);

            string expectedFilename = Path.Combine(_workingDirectory, "tr.templatestrings.json");
            Assert.IsTrue(File.Exists(expectedFilename));

            using FileStream locFileStream = File.OpenRead(expectedFilename);
            JsonDocument locFile = await JsonDocument.ParseAsync(locFileStream).ConfigureAwait(false);

            int inputIndex = 0;
            foreach (JsonProperty property in locFile.RootElement.EnumerateObject())
            {
                if (inputIndex >= InputStrings.Count)
                {
                    return;
                }

                Assert.IsTrue(property.Name == InputStrings[inputIndex].LocalizationKey);
                Assert.IsTrue(property.Value.GetString() == InputStrings[inputIndex].Value);
                inputIndex++;
            }
        }

        [TestMethod]
        public async Task ExistingTranslationsAndCommentsArePreserved()
        {
            CancellationTokenSource cts = new CancellationTokenSource(10000);
            string expectedFilename = Path.Combine(_workingDirectory, "tr.templatestrings.json");

            await File.WriteAllTextAsync(expectedFilename, @"
{
    ""name"": ""existing translations should be preserved."",
    ""_name.comment"": ""comments should be preserved.""
}",
                cts.Token).ConfigureAwait(false);

            await TemplateStringUpdater.UpdateStringsAsync(InputStrings, "en", new string[] { "tr" }, _workingDirectory, dryRun: false, NullLogger.Instance, cts.Token)
                .ConfigureAwait(false);

            string fileContent = await File.ReadAllTextAsync(expectedFilename, cts.Token).ConfigureAwait(false);

            Assert.IsTrue(fileContent.Contains("existing translations should be preserved."));
            Assert.IsTrue(fileContent.Contains("comments should be preserved."));
        }

        [TestMethod]
        public async Task ExistingValuesOfAuthoringLanguageShouldBeRemoved()
        {
            CancellationTokenSource cts = new CancellationTokenSource(10000);
            string expectedFilename = Path.Combine(_workingDirectory, "en.templatestrings.json");

            await File.WriteAllTextAsync(expectedFilename, @"
{
    ""name"": ""existing translations in authoring language should be removed.""
}",
                cts.Token).ConfigureAwait(false);

            await TemplateStringUpdater.UpdateStringsAsync(InputStrings, "en", new string[] { "en" }, _workingDirectory, dryRun: false, NullLogger.Instance, cts.Token)
                .ConfigureAwait(false);

            string fileContent = await File.ReadAllTextAsync(expectedFilename, cts.Token).ConfigureAwait(false);

            Assert.IsFalse(fileContent.Contains("existing translations in authoring language should be removed."));
        }

        private static async Task<Dictionary<string, string>> ReadTemplateStringsFromJsonFile(string path, CancellationToken cancellationToken)
        {
            using FileStream openStream = File.OpenRead(path);
            JsonSerializerOptions serializerOptions = new()
            {
                AllowTrailingCommas = true,
                MaxDepth = 1,
            };

            return await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(openStream, serializerOptions, cancellationToken)
                .ConfigureAwait(false)
                ?? new Dictionary<string, string>();
        }
    }
}
