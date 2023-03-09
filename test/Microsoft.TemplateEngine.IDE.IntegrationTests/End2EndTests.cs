﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.TestHelper;
using ITemplateMatchInfo = Microsoft.TemplateEngine.Abstractions.TemplateFiltering.ITemplateMatchInfo;
using WellKnownSearchFilters = Microsoft.TemplateEngine.Utils.WellKnownSearchFilters;

namespace Microsoft.TemplateEngine.IDE.IntegrationTests
{
    [UsesVerify]
    [Collection("Verify Tests")]
    public class End2EndTests : BootstrapperTestBase
    {
        [Fact]
        internal async Task SourceNameFormsTest()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string templateLocation = GetTestTemplateLocation("SourceNameForms");
            await InstallTemplateAsync(bootstrapper, templateLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();

            var foundTemplates = await bootstrapper.GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("TestAssets.SourceNameForms") }).ConfigureAwait(false);
            var result = await bootstrapper.CreateAsync(foundTemplates[0].Info, "MyApp.1", output, new Dictionary<string, string?>()).ConfigureAwait(false);

            Assert.Equal(CreationResultStatus.Success, result.Status);

            string targetFile = Path.Combine(output, "myapp.12.cs");
            Assert.True(File.Exists(targetFile));
            string targetFile2 = Path.Combine(output, "MyApp.1.cs");
            Assert.True(File.Exists(targetFile2));

            await Verify(File.ReadAllText(targetFile2));
        }

        [Fact]
        internal async Task ValueForms_DerivedSymbolTest()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string templateLocation = GetTestTemplateLocation("ValueForms/DerivedSymbol");
            await InstallTemplateAsync(bootstrapper, templateLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();

            var foundTemplates = await bootstrapper.GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("TestAssets.ValueForms.DerivedSymbol") }).ConfigureAwait(false);
            var result = await bootstrapper.CreateAsync(foundTemplates[0].Info, "Real.Web.App", output, new Dictionary<string, string?>()).ConfigureAwait(false);

            Assert.Equal(CreationResultStatus.Success, result.Status);

            string targetFile = Path.Combine(output, "Real.Web.App.txt");
            Assert.True(File.Exists(targetFile));

            await Verify(File.ReadAllText(targetFile));
        }

#pragma warning disable xUnit1004 // Test methods should not be skipped
        [Fact(Skip = "https://github.com/dotnet/templating/issues/5115")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
        internal async Task ValueForms_DerivedSymbolFromGeneratedSymbolTest()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string templateLocation = GetTestTemplateLocation("ValueForms/DerivedSymbolFromGeneratedSymbol");
            await InstallTemplateAsync(bootstrapper, templateLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();

            var foundTemplates = await bootstrapper.GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("TestAssets.ValueForms.DerivedSymbolFromGeneratedSymbol") }).ConfigureAwait(false);
            var result = await bootstrapper.CreateAsync(foundTemplates[0].Info, "Real.Web.App", output, new Dictionary<string, string?>()).ConfigureAwait(false);

            Assert.Equal(CreationResultStatus.Success, result.Status);

            string targetFile = Path.Combine(output, "Real.Web.App.txt");
            Assert.True(File.Exists(targetFile));

            await Verify(File.ReadAllText(targetFile));
        }

        [Fact]
        internal async Task PortAndCoalesceTest_WithFallbackInput()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string templateLocation = GetTestTemplateLocation("TemplateWithPortsAndCoalesce");
            await InstallTemplateAsync(bootstrapper, templateLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();

            IReadOnlyList<ITemplateMatchInfo> foundTemplates = await bootstrapper.GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("TestAssets.TemplateWithPortsAndCoalesce") }).ConfigureAwait(false);
            ITemplateCreationResult result = await bootstrapper.CreateAsync(foundTemplates[0].Info, "test-template", output, new Dictionary<string, string?>()).ConfigureAwait(false);

            Assert.Equal(CreationResultStatus.Success, result.Status);

            string targetFile = Path.Combine(output, "bar.cs");
            Assert.True(File.Exists(targetFile));
            string fileContent = File.ReadAllText(targetFile);
            Assert.True(Regex.Match(
                fileContent,
                """
                The port is (\d{4,5})
                The port is (\d{4,5})
                
                """).Success);
            Assert.NotEqual(
                """
                The port is 1234
                The port is 1235

                """,
                fileContent);
        }

        [Fact]
        internal async Task PortAndCoalesceTest_WithUserInput()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string templateLocation = GetTestTemplateLocation("TemplateWithPortsAndCoalesce");
            await InstallTemplateAsync(bootstrapper, templateLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();
            Dictionary<string, string?> parameters = new()
            {
                { "userPort1", "4000" },
                { "userPort2", "3000" },
            };

            IReadOnlyList<ITemplateMatchInfo> foundTemplates = await bootstrapper.GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("TestAssets.TemplateWithPortsAndCoalesce") }).ConfigureAwait(false);
            ITemplateCreationResult result = await bootstrapper.CreateAsync(foundTemplates[0].Info, "test-template", output, parameters).ConfigureAwait(false);

            Assert.Equal(CreationResultStatus.Success, result.Status);

            string targetFile = Path.Combine(output, "bar.cs");
            Assert.True(File.Exists(targetFile));
            string fileContent = File.ReadAllText(targetFile);
            Assert.Equal(
                """
                The port is 4000
                The port is 3000

                """,
                fileContent);
        }

        [Fact]
        internal async Task PortAndCoalesceTest_WithUserInputEqualToDefaults()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string templateLocation = GetTestTemplateLocation("TemplateWithPortsAndCoalesce");
            await InstallTemplateAsync(bootstrapper, templateLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();
            Dictionary<string, string?> parameters = new()
            {
                { "userPort1", "0" },
                { "userPort2", "0" },
            };

            IReadOnlyList<ITemplateMatchInfo> foundTemplates = await bootstrapper.GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("TestAssets.TemplateWithPortsAndCoalesce") }).ConfigureAwait(false);
            ITemplateCreationResult result = await bootstrapper.CreateAsync(foundTemplates[0].Info, "test-template", output, parameters).ConfigureAwait(false);

            Assert.Equal(CreationResultStatus.Success, result.Status);

            string targetFile = Path.Combine(output, "bar.cs");
            Assert.True(File.Exists(targetFile));
            string fileContent = File.ReadAllText(targetFile);
            Assert.Equal(
                """
                The port is 0
                The port is 0

                """,
                fileContent);
        }

        [Fact]
        internal async Task StringCoalesceTest_WithFallbackInput()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string templateLocation = GetTestTemplateLocation("TemplateWithStringCoalesce");
            await InstallTemplateAsync(bootstrapper, templateLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();

            IReadOnlyList<ITemplateMatchInfo> foundTemplates = await bootstrapper.GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("TestAssets.TemplateWithStringCoalesce") }).ConfigureAwait(false);
            ITemplateCreationResult result = await bootstrapper.CreateAsync(foundTemplates[0].Info, "test-template", output, new Dictionary<string, string?>()).ConfigureAwait(false);

            Assert.Equal(CreationResultStatus.Success, result.Status);

            string targetFile = Path.Combine(output, "bar.cs");
            Assert.True(File.Exists(targetFile));
            string fileContent = File.ReadAllText(targetFile);
            Assert.Equal(
                """
                var str = "fallback";
                
                """,
                fileContent);
        }

        [Fact]
        internal async Task StringCoalesceTest_WithUserInput()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string templateLocation = GetTestTemplateLocation("TemplateWithStringCoalesce");
            await InstallTemplateAsync(bootstrapper, templateLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();
            Dictionary<string, string?> parameters = new()
            {
                { "userVal", "myVal" },
            };

            IReadOnlyList<ITemplateMatchInfo> foundTemplates = await bootstrapper.GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("TestAssets.TemplateWithStringCoalesce") }).ConfigureAwait(false);
            ITemplateCreationResult result = await bootstrapper.CreateAsync(foundTemplates[0].Info, "test-template", output, parameters).ConfigureAwait(false);

            Assert.Equal(CreationResultStatus.Success, result.Status);

            string targetFile = Path.Combine(output, "bar.cs");
            Assert.True(File.Exists(targetFile));
            string fileContent = File.ReadAllText(targetFile);
            Assert.Equal(
                """
                var str = "myVal";
                
                """,
                fileContent);
        }

        [Fact]
        internal async Task StringCoalesceTest_WithEmptyUserInput()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string templateLocation = GetTestTemplateLocation("TemplateWithStringCoalesce");
            await InstallTemplateAsync(bootstrapper, templateLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();
#pragma warning disable SA1122 // Use string.Empty for empty strings
            Dictionary<string, string?> parameters = new()
            {
                { "userVal", "" },
            };
#pragma warning restore SA1122 // Use string.Empty for empty strings

            IReadOnlyList<ITemplateMatchInfo> foundTemplates = await bootstrapper.GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("TestAssets.TemplateWithStringCoalesce") }).ConfigureAwait(false);
            ITemplateCreationResult result = await bootstrapper.CreateAsync(foundTemplates[0].Info, "test-template", output, parameters).ConfigureAwait(false);

            Assert.Equal(CreationResultStatus.Success, result.Status);

            string targetFile = Path.Combine(output, "bar.cs");
            Assert.True(File.Exists(targetFile));
            string fileContent = File.ReadAllText(targetFile);
            Assert.Equal(
                """
                var str = "fallback";
                
                """,
                fileContent);
        }

        [Fact]
        internal async Task StringCoalesceTest_WithNullUserInput()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string templateLocation = GetTestTemplateLocation("TemplateWithStringCoalesce");
            await InstallTemplateAsync(bootstrapper, templateLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();
            Dictionary<string, string?> parameters = new()
            {
                { "userVal", null },
            };

            IReadOnlyList<ITemplateMatchInfo> foundTemplates = await bootstrapper.GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("TestAssets.TemplateWithStringCoalesce") }).ConfigureAwait(false);
            ITemplateCreationResult result = await bootstrapper.CreateAsync(foundTemplates[0].Info, "test-template", output, parameters).ConfigureAwait(false);

            Assert.Equal(CreationResultStatus.Success, result.Status);

            string targetFile = Path.Combine(output, "bar.cs");
            Assert.True(File.Exists(targetFile));
            string fileContent = File.ReadAllText(targetFile);
            Assert.Equal(
                """
                var str = "A";
                
                """,
                fileContent);
        }

        [Fact]
        internal async Task Test_CreateAsync_OnInvalidParamsPassed()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string templateLocation = GetTestTemplateLocation("TemplateWithPortsAndCoalesce");
            await InstallTemplateAsync(bootstrapper, templateLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();

            IReadOnlyList<ITemplateMatchInfo> foundTemplates = await bootstrapper.GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("TestAssets.TemplateWithPortsAndCoalesce") }).ConfigureAwait(false);

            Dictionary<string, string?> parameters = new()
            {
                { "userPort1", "non-int" },
                { "userPort2", string.Empty }
            };

            ITemplateCreationResult result = await bootstrapper.CreateAsync(foundTemplates[0].Info, "Test", output, parameters).ConfigureAwait(false);

            Assert.Equal(CreationResultStatus.InvalidParamValues, result.Status);
            Assert.Equal("userPort1, userPort2", result.ErrorMessage);
        }

        [Fact]
        internal async Task Test_DryRunAsync_OnInvalidParamsPassed()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string templateLocation = GetTestTemplateLocation("TemplateWithPortsAndCoalesce");
            await InstallTemplateAsync(bootstrapper, templateLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();

            IReadOnlyList<ITemplateMatchInfo> foundTemplates = await bootstrapper.GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("TestAssets.TemplateWithPortsAndCoalesce") }).ConfigureAwait(false);

            Dictionary<string, string?> parameters = new()
            {
                { "userPort1", "non-int" },
                { "userPort2", string.Empty }
            };

            ITemplateCreationResult result = await bootstrapper.GetCreationEffectsAsync(foundTemplates[0].Info, "Test", output, parameters).ConfigureAwait(false);

            Assert.Equal(CreationResultStatus.InvalidParamValues, result.Status);
            Assert.Equal("userPort1, userPort2", result.ErrorMessage);
        }

        [Fact]
        internal async Task Test_CreateAsync_OnTemplateWithConditions()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string templateLocation = GetTestTemplateLocation("TemplateWithConditions");
            await InstallTemplateAsync(bootstrapper, templateLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();

            IReadOnlyList<ITemplateMatchInfo> foundTemplates = await bootstrapper
                .GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("TestAssets.TemplateWithConditions") })
                .ConfigureAwait(false);

            Dictionary<string, string?> parameters = new()
            {
                { "B", "true" },
            };

            ITemplateCreationResult result = await bootstrapper
                .CreateAsync(foundTemplates[0].Info, "Test", output, parameters)
                .ConfigureAwait(false);

            Assert.Equal(CreationResultStatus.Success, result.Status);

            foreach (var expectResult in ExpectedOutputWithConditions())
            {
                string targetFile = Path.Combine(output, expectResult.Key);
                Assert.True(File.Exists(targetFile));
                Assert.Equal(expectResult.Value.UnixifyLineBreaks(), File.ReadAllText(targetFile).UnixifyLineBreaks());
            }
        }

        [Theory]
        [InlineData(null, "theDefaultName.cs")]
        [InlineData("fileName", "fileName.cs")]
        internal async Task Test_CreateAsync_PreferDefaultNameValidParameters(string? name, string expectedFileName)
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string templateLocation = GetTestTemplateLocation("TemplateWithPreferDefaultName");
            await InstallTemplateAsync(bootstrapper, templateLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();

            IReadOnlyList<ITemplateMatchInfo> foundTemplates = await bootstrapper
                .GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("TestAssets.TemplateWithPreferDefaultName") })
                .ConfigureAwait(false);

            // Using this parameter with no real info so bootstrapper.CreateAsync is not an ambiguous call
            Dictionary<string, string?> parameters = new()
            {
                { "some", "parameter" },
            };

            ITemplateCreationResult result = await bootstrapper
                .CreateAsync(foundTemplates[0].Info, name, output, parameters)
                .ConfigureAwait(false);

            Assert.Equal(CreationResultStatus.Success, result.Status);
            string expectedName = Path.Combine(output, expectedFileName);
            Assert.True(File.Exists(expectedName));
        }

        [Fact]
        internal async Task Test_CreateAsync_PreferDefaultNameInvalidParameters()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string templateLocation = GetTestTemplateLocation("TemplateWithPreferDefaultNameButNoDefaultName");
            await InstallTemplateAsync(bootstrapper, templateLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();

            IReadOnlyList<ITemplateMatchInfo> foundTemplates = await bootstrapper
                .GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("TestAssets.TemplateWithPreferDefaultName") })
                .ConfigureAwait(false);

            // Using this parameter with no real info so bootstrapper.CreateAsync is not an ambiguous call
            Dictionary<string, string?> parameters = new()
            {
                { "some", "parameter" },
            };

            ITemplateCreationResult result = await bootstrapper
                .CreateAsync(foundTemplates[0].Info, null, output, parameters)
                .ConfigureAwait(false);

            Assert.Equal(CreationResultStatus.TemplateIssueDetected, result.Status);
            Assert.Equal(
                "Failed to create template: the template name is not specified. Template configuration does not configure a default name that can be used when name is not specified. Specify the name for the template when instantiating or configure a default name in the template configuration.",
                result.ErrorMessage);
        }

        private Dictionary<string, string> ExpectedOutputWithConditions()
        {
            var expects = new Dictionary<string, string>
            {
                {
".dockerignore",
@"# comment bar
bar
baz
"
                },
                {
".editorconfig",
@"# comment bar
bar
baz
"
                },
                {
".gitattributes",
@"# comment bar
bar
baz
"
                },
                {
".gitignore",
@"# comment bar
bar
baz
"
                },
                {
"Dockerfile",
@"# comment bar
bar
baz
"
                },
                {
"nuget.config",
@"<!-- comment bar -->
bar
baz
"
                },
                {
"test.axaml",
@"<!-- comment A is false -->
<Application xmlns=""https://github.com/something""
             xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
             x:Class=""App""
             RequestedThemeVariant=""Default"">

    <Application.Styles>
        <!-- comment B is true -->
        <FluentTheme Mode=""Light"" />
    </Application.Styles>
</Application>"
                },
                {
"test.cake",
@"// comment bar
bar
baz
"
                },
                {
"test.md",
@"<!-- comment bar -->
bar
baz
"
                },
                {
"test.ps1",
@"# comment B true
B true
common text
"
                },
                {
"test.sln",
@"# comment bar
bar
baz
"
                },
                {
"test.yaml",
@"# comment bar
bar
baz
"
                }
            };
            return expects;
        }
    }
}
