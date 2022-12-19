// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateEngine.Tests;
using Xunit.Abstractions;

namespace Microsoft.TemplateEngine.Authoring.TemplateVerifier.IntegrationTests
{
    public class TemplateEngineSamplesTest : TestBase
    {
        private readonly ILogger _log;

        public TemplateEngineSamplesTest(ITestOutputHelper log)
        {
            _log = new XunitLoggerProvider(log).CreateLogger("TestRun");
        }

        [Theory]
        [InlineData("01-basic-template", "MyProject.Con", "sample01", null, "no args")]
        [InlineData("02-add-parameters", "MyProject.Con", "sample02", new[] { "--copyrightName", "Test Copyright", "--Title", "Test Title" }, "text args")]
        [InlineData("03-optional-page", "MyProject.StarterWeb", "sample03", new[] { "--Title", "Test Title", "--EnableContactPage", "true" }, "optional content included")]
        [InlineData("03-optional-page", "MyProject.StarterWeb", "sample03", new[] { "--Title", "Test Title" }, "optional content excluded")]
        [InlineData("04-parameter-from-list", "MyProject.Con", "sample04", new[] { "--BackgroundColor", "dimgray" }, "the parameter from the list of options")]
        [InlineData("05-multi-project", "", "sample05", new[] { "--includetest", "true" }, "the optional test project included")]
        [InlineData("05-multi-project", "", "sample05", new[] { "--includetest", "false" }, "the optional test project excluded")]
        public async void TemplateEngineSamplesProjectTest(
            string folderName,
            string projectName,
            string shortName,
            string[] args,
            string caseDescription)
        {
            _log.LogInformation($"Template with {caseDescription}");
            string workingDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName().Replace(".", string.Empty));

            //get the template location
            string templateLocation = Path.Combine(GetSamplesTemplateLocation(), folderName, projectName);

            TemplateVerifierOptions options = new TemplateVerifierOptions(templateName: shortName)
            {
                TemplatePath = templateLocation,
                OutputDirectory = workingDir,
                TemplateSpecificArgs = args
            };

            VerificationEngine engine = new VerificationEngine(_log);
            await engine.Execute(options);
        }

        private string GetSamplesTemplateLocation() => Path.Combine(CodeBaseRoot, "dotnet-template-samples", "content");
    }
}
