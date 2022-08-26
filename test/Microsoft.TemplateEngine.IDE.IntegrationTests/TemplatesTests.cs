// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.TestHelper;
using ITemplateMatchInfo = Microsoft.TemplateEngine.Abstractions.TemplateFiltering.ITemplateMatchInfo;
using WellKnownSearchFilters = Microsoft.TemplateEngine.Utils.WellKnownSearchFilters;

namespace Microsoft.TemplateEngine.IDE.IntegrationTests
{
    public class TemplatesTests : BootstrapperTestBase, IClassFixture<PackageManager>
    {
        private PackageManager _packageManager;

        public TemplatesTests(PackageManager packageManager)
        {
            _packageManager = packageManager;
        }

        [Fact]
        internal async Task ConsoleBasicTest()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string packageLocation = await _packageManager.GetNuGetPackage("Microsoft.DotNet.Common.ProjectTemplates.5.0").ConfigureAwait(false);
            await InstallTemplateAsync(bootstrapper, packageLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();
            IReadOnlyList<ITemplateMatchInfo> foundTemplates = await bootstrapper.GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("console") }).ConfigureAwait(false);
            ITemplateCreationResult result = await bootstrapper.CreateAsync(foundTemplates[0].Info, "test", output, new Dictionary<string, string?>()).ConfigureAwait(false);
            Assert.Equal(2, result.CreationResult?.PrimaryOutputs.Count);
            Assert.Equal(2, result.CreationResult?.PostActions.Count);

            //return VerifyFolder(output)
            //    .Ignore("obj/**/*")
            //    .Ignore("bin/**/*")
            //    .ScrubGuids()
            //    .AddScrubber(MyCustomScrubber);

            //content of the folder is:
            // - obj
            // - Program.cs
            // - test.csproj
        }

        [Fact]
        internal async Task WebAppBasicTest()
        {
            using Bootstrapper bootstrapper = GetBootstrapper();
            string packageLocation = await _packageManager.GetNuGetPackage("Microsoft.DotNet.Web.ProjectTemplates.5.0").ConfigureAwait(false);
            await InstallTemplateAsync(bootstrapper, packageLocation).ConfigureAwait(false);

            string output = TestUtils.CreateTemporaryFolder();
            IReadOnlyList<ITemplateMatchInfo> foundTemplates = await bootstrapper.GetTemplatesAsync(new[] { WellKnownSearchFilters.NameFilter("webapp") }).ConfigureAwait(false);
            ITemplateCreationResult result = await bootstrapper.CreateAsync(foundTemplates[0].Info, "test", output, new Dictionary<string, string?>()).ConfigureAwait(false);
            Assert.Equal(1, result.CreationResult?.PrimaryOutputs.Count);
            Assert.Equal(1, result.CreationResult?.PostActions.Count);

            //return VerifyFolder(output)
            //    .Ignore("obj/**/*")
            //    .Ignore("bin/**/*")
            //    .DoNotCompareContent(wwwroot/**/*)
            //    .ScrubGuids()
            //    .AddScrubber(MyCustomScrubber);

            //content of the folder is:
            // - obj
            // - Pages
            // - Properties
            // - wwwroot
            // - appsettings.Development.json
            // - appsettings.json
            // - Program.cs
            // - Startup.cs
            // - test.csproj
        }
    }
}
