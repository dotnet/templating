// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ConfigModel;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.UnitTests.TemplateConfigTests
{
    public class GenericTests
    {
        private static readonly string TestTemplate = /*lang=json*/ """
            {
              "author": "Test Asset",
              "classifications": [ "Test Asset" ],
              "name": "TemplateWithSourceName",
              "generatorVersions": "[1.0.0.0-*)",
              "groupIdentity": "TestAssets.TemplateWithSourceName",
              "precedence": "100",
              "identity": "TestAssets.TemplateWithSourceName",
              "shortName": "TestAssets.TemplateWithSourceName",
              "sourceName": "bar",
              "primaryOutputs": [
                {
                  "path": "bar.cs"
                },
                {
                  "path": "bar/bar.cs"
                },
              ]
            }
            """;

        [Fact]
        public void CanReadTemplateFromString()
        {
            TemplateConfigModel templateConfigModel = TemplateConfigModel.FromString(TestTemplate);

            Assert.Equal("Test Asset", templateConfigModel.Author);
            Assert.Equal("TemplateWithSourceName", templateConfigModel.Name);
            Assert.Equal("bar", templateConfigModel.SourceName);
            Assert.Equal(2, templateConfigModel.PrimaryOutputs.Count);
            Assert.Equal(new[] { "bar.cs", "bar/bar.cs" }, templateConfigModel.PrimaryOutputs.Select(po => po.Path).OrderBy(po => po));
        }

        [Fact]
        public void CanReadTemplateFromStream()
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(TestTemplate ?? string.Empty));
            TemplateConfigModel templateConfigModel = TemplateConfigModel.FromStream(stream);

            Assert.Equal("Test Asset", templateConfigModel.Author);
            Assert.Equal("TemplateWithSourceName", templateConfigModel.Name);
            Assert.Equal("bar", templateConfigModel.SourceName);
            Assert.Equal(2, templateConfigModel.PrimaryOutputs.Count);
            Assert.Equal(new[] { "bar.cs", "bar/bar.cs" }, templateConfigModel.PrimaryOutputs.Select(po => po.Path).OrderBy(po => po));
        }

        /// <summary>
        /// Regression test: a template.json with duplicate property keys in a nested section (e.g. "forms")
        /// should load successfully rather than throwing ArgumentException.
        /// See https://github.com/dotnet/templating/issues/7628.
        /// </summary>
        [Fact]
        public void CanReadTemplateWithDuplicateJsonKeys()
        {
            // The MAUI templates had a bug where "empty" appeared twice in the "forms" section.
            // JsonObject.InitializeDictionary() throws ArgumentException on duplicate keys.
            // The template engine should be lenient and load the template anyway.
            string templateWithDuplicateKeys = /*lang=json*/ """
                {
                  "author": "Test Asset",
                  "classifications": [ "Test Asset" ],
                  "name": "TemplateWithDuplicateKeys",
                  "identity": "TestAssets.TemplateWithDuplicateKeys",
                  "shortName": "TestAssets.TemplateWithDuplicateKeys",
                  "forms": {
                    "empty": { "identifier": "empty" },
                    "empty": { "identifier": "empty" }
                  }
                }
                """;

            // Should not throw even though "forms" has duplicate "empty" keys.
            TemplateConfigModel model = TemplateConfigModel.FromString(templateWithDuplicateKeys);

            Assert.Equal("TemplateWithDuplicateKeys", model.Name);
            // The "empty" form should be present (last-write-wins semantics for the duplicate key).
            Assert.Contains("empty", model.Forms.Keys);
        }

        /// <summary>
        /// Regression test: a template.json with duplicate symbol keys should load successfully.
        /// </summary>
        [Fact]
        public void CanReadTemplateWithDuplicateSymbolKeys()
        {
            string templateWithDuplicateSymbols = /*lang=json*/ """
                {
                  "author": "Test Asset",
                  "classifications": [ "Test Asset" ],
                  "name": "TemplateWithDuplicateSymbols",
                  "identity": "TestAssets.TemplateWithDuplicateSymbols",
                  "shortName": "TestAssets.TemplateWithDuplicateSymbols",
                  "symbols": {
                    "Framework": {
                      "type": "parameter",
                      "description": "The target framework for the project.",
                      "datatype": "choice",
                      "choices": [
                        { "choice": "net9.0", "description": "Target net9.0" }
                      ],
                      "defaultValue": "net9.0"
                    },
                    "Framework": {
                      "type": "parameter",
                      "description": "Duplicate symbol definition.",
                      "datatype": "choice",
                      "choices": [
                        { "choice": "net9.0", "description": "Target net9.0" }
                      ],
                      "defaultValue": "net9.0"
                    }
                  }
                }
                """;

            // Should not throw even though "symbols" has duplicate "Framework" keys.
            TemplateConfigModel model = TemplateConfigModel.FromString(templateWithDuplicateSymbols);

            Assert.Equal("TemplateWithDuplicateSymbols", model.Name);
            Assert.Contains(model.Symbols, s => s.Name == "Framework");
        }
    }
}
