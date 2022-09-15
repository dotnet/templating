// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using FluentAssertions;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier;
using Microsoft.TemplateEngine.TestHelper;
using Microsoft.TemplateEngine.TestHelper.Commands;
using Xunit.Abstractions;

namespace Microsoft.TemplateEngine.Authoring.CLI.IntegrationTests
{
    [UsesVerify]
    public class VerifyCommandTests
    {
        private readonly ITestOutputHelper _log;

        public VerifyCommandTests(ITestOutputHelper log)
        {
            _log = log;
        }

        [Fact]
        public void VerifyCommandFullDevLoop()
        {
            string workingDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string expectationsDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string templateDir = "path with spaces";

            var cmd = new BasicCommand(
                _log,
                "dotnet",
                Path.GetFullPath("Microsoft.TemplateEngine.Authoring.CLI.dll"),
                "verify",
                "console",
                "--template-args",
                "--use-program-main -o \"" + templateDir + "\"  --no-restore",
                "--verify-std",
                "-o",
                workingDir,
                "--expectations-directory",
                expectationsDir,
                "--disable-diff-tool");

            cmd.Execute()
                .Should()
                .ExitWith((int)TemplateVerificationErrorCode.VerificationFailed)
                .And.HaveStdErrContaining("Verification Failed.");

            // Assert template created
            Directory.Exists(Path.Combine(workingDir, templateDir)).Should().BeTrue();
            File.Exists(Path.Combine(workingDir, templateDir, "console.csproj")).Should().BeTrue();
            File.Exists(Path.Combine(workingDir, templateDir, "Program.cs")).Should().BeTrue();

            // Assert verification files created
            Directory.Exists(expectationsDir).Should().BeTrue();
            File.Exists(Path.Combine(expectationsDir, "console.console.csproj.received.csproj")).Should().BeTrue();
            File.Exists(Path.Combine(expectationsDir, "console.Program.cs.received.cs")).Should().BeTrue();
            File.Exists(Path.Combine(expectationsDir, "console.StdOut.received.txt")).Should().BeTrue();
            File.Exists(Path.Combine(expectationsDir, "console.StdErr.received.txt")).Should().BeTrue();
            // .verified files are only created when diff tool is used - that is however turned off in CI
            //File.Exists(Path.Combine(expectationsDir, "console.console.csproj.verified.csproj")).Should().BeTrue();
            //File.Exists(Path.Combine(expectationsDir, "console.Program.cs.verified.cs")).Should().BeTrue();
            //File.Exists(Path.Combine(expectationsDir, "console.StdOut.verified.txt")).Should().BeTrue();
            //File.Exists(Path.Combine(expectationsDir, "console.StdErr.verified.txt")).Should().BeTrue();
            Directory.GetFiles(expectationsDir).Length.Should().Be(4);

            // .verified files are only created when diff tool is used - that is however turned off in CI
            //File.ReadAllText(Path.Combine(expectationsDir, "console.console.csproj.verified.csproj")).Should().BeEmpty();
            //File.ReadAllText(Path.Combine(expectationsDir, "console.Program.cs.verified.cs")).Should().BeEmpty();
            //File.ReadAllText(Path.Combine(expectationsDir, "console.StdOut.verified.txt")).Should().BeEmpty();
            //File.ReadAllText(Path.Combine(expectationsDir, "console.StdErr.verified.txt")).Should().BeEmpty();
            File.ReadAllText(Path.Combine(expectationsDir, "console.console.csproj.received.csproj").UnixifyLineBreaks()).Should()
                .BeEquivalentTo(File.ReadAllText(Path.Combine(workingDir, templateDir, "console.csproj")).UnixifyLineBreaks());
            File.ReadAllText(Path.Combine(expectationsDir, "console.Program.cs.received.cs").UnixifyLineBreaks()).Should()
                .BeEquivalentTo(File.ReadAllText(Path.Combine(workingDir, templateDir, "Program.cs")).UnixifyLineBreaks());

            // Accept changes
            File.Delete(Path.Combine(expectationsDir, "console.console.csproj.verified.csproj"));
            File.Delete(Path.Combine(expectationsDir, "console.Program.cs.verified.cs"));
            File.Delete(Path.Combine(expectationsDir, "console.StdOut.verified.txt"));
            File.Delete(Path.Combine(expectationsDir, "console.StdErr.verified.txt"));
            File.Move(
                Path.Combine(expectationsDir, "console.console.csproj.received.csproj"),
                Path.Combine(expectationsDir, "console.console.csproj.verified.csproj"));
            File.Move(
                Path.Combine(expectationsDir, "console.Program.cs.received.cs"),
                Path.Combine(expectationsDir, "console.Program.cs.verified.cs"));
            File.Move(
                Path.Combine(expectationsDir, "console.StdOut.received.txt"),
                Path.Combine(expectationsDir, "console.StdOut.verified.txt"));
            File.Move(
                Path.Combine(expectationsDir, "console.StdErr.received.txt"),
                Path.Combine(expectationsDir, "console.StdErr.verified.txt"));

            // And run again same scenario - verification should succeed now
            string workingDir2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var cmd2 = new BasicCommand(
                _log,
                "dotnet",
                Path.GetFullPath("Microsoft.TemplateEngine.Authoring.CLI.dll"),
                "verify",
                "console",
                "--template-args",
                "--use-program-main -o \"" + templateDir + "\"  --no-restore",
                "--verify-std",
                "-o",
                workingDir2,
                "--expectations-directory",
                expectationsDir);

            cmd2.Execute()
                .Should()
                .Pass()
                .And.HaveStdOutContaining("Running the verification of console.")
                .And.NotHaveStdErr();

            Directory.Delete(workingDir, true);
            Directory.Delete(workingDir2, true);
            Directory.Delete(expectationsDir, true);
        }
    }
}
