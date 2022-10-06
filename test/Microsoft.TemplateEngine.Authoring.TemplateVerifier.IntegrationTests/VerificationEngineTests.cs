﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.TestHelper;
using Xunit.Abstractions;

namespace Microsoft.TemplateEngine.Authoring.TemplateVerifier.IntegrationTests
{
    public class VerificationEngineTests
    {
        private readonly ILogger _log;

        public VerificationEngineTests(ITestOutputHelper log)
        {
            _log = new XunitLoggerProvider(log).CreateLogger("TestRun");
        }

        [Fact]
        public async void VerificationEngineFullDevLoop()
        {
            string workingDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName().Replace(".", string.Empty));
            string expectationsDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName().Replace(".", string.Empty));
            string templateDir = "path with spaces";

            TemplateVerifierOptions options = new TemplateVerifierOptions(templateName: "console")
            {
                TemplateSpecificArgs = new string[] { "--use-program-main", "-o", templateDir, "--no-restore" },
                DisableDiffTool = true,
                ExpectationsDirectory = expectationsDir,
                OutputDirectory = workingDir,
                VerifyCommandOutput = true,
                UniqueFor = UniqueForOption.OsPlatform | UniqueForOption.OsPlatform,
            };

            VerificationEngine engine = new VerificationEngine(_log);
            Func<Task> executeTask = () => engine.Execute(options);
            await executeTask
                .Should()
                .ThrowAsync<TemplateVerificationException>()
                .Where(e => e.TemplateVerificationErrorCode == TemplateVerificationErrorCode.VerificationFailed);

            // Assert template created
            Directory.Exists(Path.Combine(workingDir, templateDir)).Should().BeTrue();
            File.Exists(Path.Combine(workingDir, templateDir, "console.csproj")).Should().BeTrue();
            File.Exists(Path.Combine(workingDir, templateDir, "Program.cs")).Should().BeTrue();

            // Assert verification files created
            Directory.Exists(expectationsDir).Should().BeTrue();
            Directory.GetDirectories(expectationsDir).Length.Should().Be(2);
            //for simplicity move to the received dir
            expectationsDir = Directory.GetDirectories(expectationsDir).Single(d => d.EndsWith(".received", StringComparison.Ordinal));
            File.Exists(Path.Combine(expectationsDir, templateDir, "console.csproj")).Should().BeTrue();
            File.Exists(Path.Combine(expectationsDir, templateDir, "Program.cs")).Should().BeTrue();
            File.Exists(Path.Combine(expectationsDir, "std-streams", "stdout.txt")).Should().BeTrue();
            File.Exists(Path.Combine(expectationsDir, "std-streams", "stderr.txt")).Should().BeTrue();
            Directory.GetFiles(expectationsDir, "*", SearchOption.AllDirectories).Length.Should().Be(4);

            File.ReadAllText(Path.Combine(expectationsDir, templateDir, "console.csproj").UnixifyLineBreaks()).Should()
                .BeEquivalentTo(File.ReadAllText(Path.Combine(workingDir, templateDir, "console.csproj")).UnixifyLineBreaks());
            File.ReadAllText(Path.Combine(expectationsDir, templateDir, "Program.cs").UnixifyLineBreaks()).Should()
                .BeEquivalentTo(File.ReadAllText(Path.Combine(workingDir, templateDir, "Program.cs")).UnixifyLineBreaks());

            // Accept changes
            string verifiedDir = expectationsDir.Replace(".received", ".verified", StringComparison.Ordinal);
            Directory.Delete(verifiedDir, false);
            Directory.Move(expectationsDir, verifiedDir);

            //reset the expectations dir to where it was before previous run
            expectationsDir = Path.GetDirectoryName(expectationsDir)!;

            // And run again same scenario - verification should succeed now
            string workingDir2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            TemplateVerifierOptions options2 = new TemplateVerifierOptions(templateName: "console")
            {
                TemplateSpecificArgs = new string[] { "--use-program-main", "-o", templateDir, "--no-restore" },
                DisableDiffTool = true,
                ExpectationsDirectory = expectationsDir,
                OutputDirectory = workingDir2,
                VerifyCommandOutput = true,
                UniqueFor = UniqueForOption.OsPlatform | UniqueForOption.OsPlatform,
            };

            Func<Task> executeTask2 = () => engine.Execute(options2);
            await executeTask2
                .Should()
                .NotThrowAsync();

            Directory.Delete(workingDir, true);
            Directory.Delete(workingDir2, true);
            Directory.Delete(expectationsDir, true);
        }

        [Fact]
        public async void VerificationEngineCustomVerifier()
        {
            string workingDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName().Replace(".", string.Empty));
            string templateDir = "path with spaces";

            TemplateVerifierOptions options = new TemplateVerifierOptions(templateName: "console")
            {
                TemplateSpecificArgs = new string[] { "--use-program-main", "-o", templateDir, "--no-restore" },
                DisableDiffTool = true,
                OutputDirectory = workingDir,
                VerificationExcludePatterns = new[] { "*.cs" },
                VerifyCommandOutput = true,
                UniqueFor = UniqueForOption.OsPlatform | UniqueForOption.OsPlatform,
            }
                .WithCustomScrubbers(
                    ScrubbersDefinition.Empty
                        .AddScrubber(sb => sb.Replace("Donut", "Veggies"), "txt")
                        .AddScrubber(sb => sb.Replace(DateTime.UtcNow.ToString("yyyy-MM-dd"), "2000-01-01"))
                )
                .WithCustomDirectoryVerifier(
                    async (content, contentFetcher) =>
                    {
                        await foreach (var file in contentFetcher.Value)
                        {
                            if (Path.GetExtension(file.FilePath).Equals(".cs"))
                            {
                                throw new Exception(".cs files should be excluded per VerificationExcludePatterns");
                            }

                            if (Path.GetFileName(file.FilePath).Equals("stdout.txt", StringComparison.OrdinalIgnoreCase)
                                && !file.ScrubbedContent.Contains("Console"))
                            {
                                throw new Exception("stdout should contain 'Console'");
                            }

                            if (Path.GetExtension(file.FilePath).Equals(".csproj")
                                && !file.ScrubbedContent.Contains("<ImplicitUsings>enable</ImplicitUsings>"))
                            {
                                throw new Exception("Implicit usings should be used");
                            }
                        }
                    }
                    );

            VerificationEngine engine = new VerificationEngine(_log);
            Func<Task> executeTask = () => engine.Execute(options);
            await executeTask
                .Should()
                .NotThrowAsync();

            Directory.Delete(workingDir, true);
        }
    }
}
