// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Core.UnitTests
{
    public class StringExtractorTests
    {
        private const string SimpleTemplateJson = @"{
    ""$schema"": ""http://json.schemastore.org/template"",
    ""author"": ""Microsoft"",
    ""classifications"": [""Common"", ""Library""],
    ""name"": ""Class library"",
    ""generatorVersions"": ""[1.0.0.0-*)"",
    ""description"": ""dEscRiPtiON: ,./|\\<>{}!@#$%^&*()_+-=? 12 äÄßöÖüÜçÇğĞıIİşŞ"",
    ""groupIdentity"": ""Microsoft.Common.Library"",
    ""precedence"": ""7000"",
    ""identity"": ""Microsoft.Common.Library.CSharp.5.0"",
    ""shortName"": ""classlib""
}
";
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

        [Fact]
        public void FirstLevelStringsAreExtracted()
        {
            var strings = ExtractStrings(SimpleTemplateJson, out _);

            Assert.Contains(strings, s => s.Identifier == "..name" && s.LocalizationKey == "name" && s.Value == "Class library");
            Assert.Contains(strings, s => s.Identifier == "..description" && s.LocalizationKey == "description" && s.Value == "dEscRiPtiON: ,./|\\<>{}!@#$%^&*()_+-=? 12 äÄßöÖüÜçÇğĞıIİşŞ");
        }

        [Fact]
        public void CertainStringsAreOmitted()
        {
            var strings = ExtractStrings(SimpleTemplateJson, out _);

            Assert.DoesNotContain(strings, s => s.Identifier == "..$schema" && s.LocalizationKey == "author" && s.Value == "Microsoft");
            Assert.DoesNotContain(strings, s => s.Identifier == "..classification" || s.LocalizationKey == "classification");
            Assert.DoesNotContain(strings, s => s.Identifier == "..groupIdentity" || s.LocalizationKey == "groupIdentity");
        }

        [Fact]
        public void DefaultAuthoringLanguageIsEnglish()
        {
            _ = ExtractStrings(SimpleTemplateJson, out string language);

            Assert.Equal("en", language);
        }

        [Fact]
        public void SymbolsAreExtracted()
        {
            var strings = ExtractStrings(ComplexTemplateJson, out _);

            Assert.Contains(strings, s => s.Identifier == "..symbols.targetframeworkoverride.description" && s.LocalizationKey == "symbols.TargetFrameworkOverride.description" && s.Value == "tfm description");
            Assert.Contains(strings, s => s.Identifier == "..symbols.targetframeworkoverride.displayname" && s.LocalizationKey == "symbols.TargetFrameworkOverride.displayName" && s.Value == "tfm display name");
            Assert.Contains(strings, s => s.Identifier == "..symbols.framework.description" && s.LocalizationKey == "symbols.Framework.description" && s.Value == "framework description");
            Assert.Contains(strings, s => s.Identifier == "..symbols.framework.displayname" && s.LocalizationKey == "symbols.Framework.displayName" && s.Value == "framework display name");
        }

        [Fact]
        public void SymbolChoicesAreExtracted()
        {
            var strings = ExtractStrings(ComplexTemplateJson, out _);

            Assert.Contains(strings, s => s.Identifier == "..symbols.framework.choices.0.description" && s.LocalizationKey == "symbols.Framework.choices.net5_0.description" && s.Value == "Target net5.0");
            Assert.Contains(strings, s => s.Identifier == "..symbols.framework.choices.0.displayname" && s.LocalizationKey == "symbols.Framework.choices.net5_0.displayName" && s.Value == "net5.0 display name");
            Assert.Contains(strings, s => s.Identifier == "..symbols.framework.choices.2.description" && s.LocalizationKey == "symbols.Framework.choices.netstandard2_0.description" && s.Value == "Target netstandard2.0");
            Assert.Contains(strings, s => s.Identifier == "..symbols.framework.choices.2.displayname" && s.LocalizationKey == "symbols.Framework.choices.netstandard2_0.displayName" && s.Value == "netstandard2.0 display name");
        }

        [Fact]
        public void PostActionsAreExtracted()
        {
            var strings = ExtractStrings(ComplexTemplateJson, out _);

            Assert.Contains(strings, s => s.Identifier == "..postactions.0.description" && s.LocalizationKey == "postActions[0].description" && s.Value == "Restore NuGet packages required by this project.");
            Assert.Contains(strings, s => s.Identifier == "..postactions.1.description" && s.LocalizationKey == "postActions[1].description" && s.Value == "Opens Class1.cs in the editor");
        }

        [Fact]
        public void ManualInstructionsAreExtracted()
        {
            var strings = ExtractStrings(ComplexTemplateJson, out _);

            Assert.Contains(strings, s => s.Identifier == "..postactions.0.manualinstructions.0.text" && s.LocalizationKey == "postActions[0].manualInstructions[0].text" && s.Value == "Run 'dotnet restore'");
        }

        private static IReadOnlyList<TemplateString> ExtractStrings(string json, out string language)
        {
            JsonDocument jsonDocument = JsonDocument.Parse(json, new JsonDocumentOptions()
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });

            TemplateStringExtractor templateStringExtractor = new (jsonDocument);
            return templateStringExtractor.ExtractStrings(out language);
        }
    }
}
