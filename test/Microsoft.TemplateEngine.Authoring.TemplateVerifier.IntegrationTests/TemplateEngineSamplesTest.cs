// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Authoring.TemplateApiVerifier;
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
        [InlineData("01-basic-template", "sample01", null, "no args")]
        [InlineData("02-add-parameters", "sample02", new[] { "copyrightName", "Test Copyright", "title", "Test Title" }, "text args")]
        [InlineData("03-optional-page", "sample03", new[] { "enableContactPage", "true" }, "optional content included")]
        [InlineData("03-optional-page", "sample03", null, "optional content excluded")]
        [InlineData("04-parameter-from-list", "sample04", new[] { "BackgroundColor", "dimgray" }, "the parameter from the list of options")]
        [InlineData("05-multi-project", "sample05", new[] { "--includetest", "true" }, "the optional test project included")]
        [InlineData("05-multi-project", "sample05", new[] { "--includetest", "false" }, "the optional test project excluded")]
        [InlineData("06-console-csharp-fsharp", "sample06", new[] { "--language", "F#" }, "the F# lang parameter that creates a corresponded project")]
        [InlineData("06-console-csharp-fsharp", "sample06", new[] { "--language", "C#" }, "the C# lang parameter that creates a corresponded project")]
        [InlineData("07-param-with-custom-short-name", "sample07", new[] { "preferNameDirectory", "true" }, "custom name directory")]
        [InlineData("08-restore-on-create", "sample08", null, "restore on create")]
        [InlineData("09-replace-onlyif-after", "sample09", new[] { "backgroundColor", "grey" }, "replacing with onlyif condition")]
        [InlineData("10-symbol-from-date", "sample10", null, "usage of date generator")]
        [InlineData("11-change-string-casing", "sample11", null, "usage of casing generator")]
        [InlineData("13-constant-value", "sample13", null, "replacing of constant value")]
        [InlineData("15-computed-symbol", "sample15", null, "usage computed symbols")]
        [InlineData("16-string-value-transform", "sample16", null, "usage of string value transformator")]
        public async void TemplateEngineSamplesProjectTest(
            string folderName,
            string shortName,
            string[] args,
            string caseDescription)
        {
            _log.LogInformation($"Template with {caseDescription}");

            //get the template location
            string templateLocation = Path.Combine(GetSamplesTemplateLocation(), folderName);

            TemplateVerifierOptions options = new TemplateVerifierOptions(templateName: shortName)
            {
                TemplatePath = templateLocation
            }
             .WithInstantiationThroughTemplateCreatorApi(GetTemplateArgs(args))
             .WithCustomScrubbers(
                ScrubbersDefinition.Empty
                .AddScrubber(sb => sb.Replace(DateTime.Now.ToString("MM/dd/yyyy"), "**/**/****")));

            VerificationEngine engine = new VerificationEngine(_log);
            await engine.Execute(options)
                .ConfigureAwait(false);
        }

        private string GetSamplesTemplateLocation() => Path.Combine(CodeBaseRoot, "dotnet-template-samples", "content");

        private Dictionary<string, string?> GetTemplateArgs(string[] args)
        {
            var templateArgs = new Dictionary<string, string?>();
            if (args != null)
            {
                for (int indx = 0; indx < args.Length; indx += 2)
                {
                    templateArgs.Add(args[indx], args[indx + 1]);
                }
            }

            return templateArgs;
        }
    }
}
