// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FakeItEasy;
using FluentAssertions;

namespace Microsoft.TemplateEngine.Authoring.TemplateVerifier.UnitTests
{
    [UsesVerify]
    public class VerificationEngineTests
    {
        [Fact]
        public async void CreateVerificationTaskWithCustomScrubbersAndVerifier()
        {
            string verifyLocation = "foo\\bar\\baz";

            Dictionary<string, string> files = new Dictionary<string, string>()
            {
                { "Program.cs", "aa bb cc" },
                { "Subfolder\\Class.cs", "123 456 789 aa" },
                { "out.dll", "a1 b2" }
            };

            IFileSystem fileSystem = A.Fake<IFileSystem>();
            A.CallTo(() => fileSystem.EnumerateFiles(verifyLocation, "*", SearchOption.AllDirectories)).Returns(files.Keys);
            A.CallTo(() => fileSystem.ReadAllTextAsync(A<string>._)).ReturnsLazily((string fileName) => Task.FromResult(files[fileName]));

            Dictionary<string, string> resultContents = new Dictionary<string, string>();

            TemplateVerifierOptions options = new TemplateVerifierOptions(templateName: "console")
            {
                TemplateSpecificArgs = null,
                DisableDiffTool = null,
                OutputDirectory = verifyLocation,
                VerificationExcludePatterns = new[] { "*.dll" },
                VerifyCommandOutput = null,
                UniqueFor = null,
            }
                .WithCustomScrubbers(
                    ScrubbersDefinition.Empty
                        .AddScrubber(sb => sb.Replace("bb", "xx"), "cs")
                        .AddScrubber(sb => sb.Replace("cc", "yy"), "dll")
                        .AddScrubber(sb => sb.Replace("123", "yy"), "dll")
                        .AddScrubber(sb => sb.Replace("aa", "zz"))
                )
                .WithCustomDirectoryVerifier(
                    async (content, contentFetcher) =>
                    {
                        await foreach (var file in contentFetcher.Value)
                        {
                            resultContents[file.FilePath] = file.ScrubbedContent;
                        }
                    }
                    );

            await VerificationEngine.CreateVerificationTask(verifyLocation, options, fileSystem);

            resultContents.Keys.Count.Should().Be(2);
            resultContents["Program.cs"].Should().BeEquivalentTo("zz xx cc");
            resultContents["Subfolder\\Class.cs"].Should().BeEquivalentTo("123 456 789 zz");
        }
    }
}
