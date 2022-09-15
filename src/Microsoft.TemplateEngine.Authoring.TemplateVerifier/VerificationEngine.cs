// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TemplateEngine.TestHelper.Commands;
using Microsoft.TemplateEngine.Utils;
using Xunit.Abstractions;

namespace Microsoft.TemplateEngine.Authoring.TemplateVerifier
{
    public class VerificationEngine
    {
        private static readonly IReadOnlyList<string> _defaultVerificationExcludePatterns = new List<string>()
        {
            @"obj/*",
            @"obj\*",
            @"bin/*",
            @"bin\*",
            "*.exe",
            "*.dll",
            "*.",
            "*.exe",
        };

        private readonly TemplateVerifierOptions _options;
        private readonly /*ILogger<VerificationEngine>*/ILogger _logger;

        public VerificationEngine(IOptions<TemplateVerifierOptions> optionsAccessor, ILogger logger)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _options = optionsAccessor.Value;
            _logger = logger;
        }

        public async Task Execute(CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(_options.TemplatePath))
            {
                throw new TemplateVerificationException("Custom template path not yet supported.", TemplateVerificationErrorCode.InternalError);
            }

            if (string.IsNullOrEmpty(_options.TemplateName))
            {
                throw new TemplateVerificationException("Template name not supplied - but custom template path is not yet supported.", TemplateVerificationErrorCode.InternalError);
            }

            // Create temp folder and instantiate there
            string workingDir = _options.OutputDirectory ?? Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            if (Directory.Exists(workingDir) && Directory.EnumerateFileSystemEntries(workingDir).Any())
            {
                throw new TemplateVerificationException("Working directory already exists and is not empty.", TemplateVerificationErrorCode.WorkingDirectoryExists);
            }

            Directory.CreateDirectory(workingDir);

            List<string> cmdArgs = new();
            if (!string.IsNullOrEmpty(_options.DotnetNewCommandAssemblyPath))
            {
                cmdArgs.Add(_options.DotnetNewCommandAssemblyPath);
            }
            cmdArgs.Add("new");
            cmdArgs.Add(_options.TemplateName);
            if (_options.TemplateSpecificArgs != null)
            {
                cmdArgs.AddRange(_options.TemplateSpecificArgs);
            }
            cmdArgs.Add("--debug:ephemeral-hive");
            // let's make sure the template outputs are named deterministically
            cmdArgs.Add("-n");
            cmdArgs.Add(_options.TemplateName);

            // TODO: export and use impl from sdk
            CommandResult commandResult =
                new DotnetCommand(new LoggerProxy(_logger), "dotnet", cmdArgs.ToArray())
                    .WithWorkingDirectory(workingDir)
                    .Execute();

            if (_options.IsCommandExpectedToFail ?? false)
            {
                commandResult.Should().Fail();
            }
            else
            {
                var assertion = commandResult.Should().Pass();
                // We do not expect stderr in passing command.
                // However if verification of stdout and stderr is opted-in - we will let that verification validate the stderr content
                if (!(_options.VerifyCommandOutput ?? false))
                {
                    assertion.And.NotHaveStdErr();
                }
            }

            await VerifyResult(_options, commandResult).ConfigureAwait(false);
        }

        private static void DummyMethod()
        { }

        private async Task VerifyResult(TemplateVerifierOptions args, CommandResult commandResult)
        {
            // Customize diff output of verifier
            VerifyDiffPlex.Initialize(OutputType.Compact);

            UsesVerifyAttribute a = new UsesVerifyAttribute();
            // https://github.com/VerifyTests/Verify/blob/d8cbe38f527d6788ecadd6205c82803bec3cdfa6/src/Verify.Xunit/Verifier.cs#L10
            //  need to simulate execution from tests
            var v = DummyMethod;
            MethodInfo mi = v.Method;
            a.Before(mi);

            List<string> exclusionsList = (args.DisableDefaultVerificationExcludePatterns ?? false)
                ? new()
                : new(_defaultVerificationExcludePatterns);

            if (args.VerificationExcludePatterns != null)
            {
                exclusionsList.AddRange(args.VerificationExcludePatterns);
            }

            List<Glob> globs = exclusionsList.Select(pattern => Glob.Parse(pattern)).ToList();

            IEnumerable<string> filesToVerify =
                Directory.EnumerateFiles(commandResult.StartInfo.WorkingDirectory, "*", SearchOption.AllDirectories);
            if (_options.VerifyCommandOutput ?? false)
            {
                filesToVerify = filesToVerify.Concat(SpecialFiles.FileNames);
            }

            List<string> verificationErrors = new List<string>();

            // run verification
            foreach (string filePath in filesToVerify)
            {
                if (globs.Any(g => g.IsMatch(filePath)))
                {
                    continue;
                }

                //TODO: this wont be needed
                VerifierSettings.DerivePathInfo(
                    (sourceFile, projectDirectory, type, method) => new(
                        directory: args.ExpectationsDirectory ?? "VerifyExpectations",
                        typeName: args.TemplateName,
                        //TODO: would this actually be needed - then we'd need to encode relative path to file here as well
                        // (as single template can have multiple files with same name)
                        methodName: Path.GetFileName(filePath)));
                try
                {
                    SettingsTask defaultVerifyTask;

                    if (SpecialFiles.IsSpecialFile(filePath))
                    {
                        defaultVerifyTask = Verifier.Verify(SpecialFiles.IsStdOut(filePath) ? commandResult.StdOut : commandResult.StdErr);
                    }
                    else
                    {
                        defaultVerifyTask = Verifier.VerifyFile(filePath);
                    }

                    if (_options.CustomScrubber != null)
                    {
                        defaultVerifyTask = defaultVerifyTask.AddScrubber(sb => _options.CustomScrubber(filePath, sb));
                    }

                    if (_options.DisableDiffTool ?? false)
                    {
                        defaultVerifyTask = defaultVerifyTask.DisableDiff();
                    }

                    Task verifyTask = _options.CustomVerifier != null
                        ? _options.CustomVerifier(filePath, GetVerificationContent(filePath, commandResult), defaultVerifyTask)
                        : defaultVerifyTask;

                    await verifyTask.ConfigureAwait(false);
                }
                //TODO: VerifyException is not public now - so either use reflection or get the Verify package updated
                //catch (VerifyException e)
                //{
                //    throw;
                //}
                catch (Exception e)
                {
                    if (e.GetType().Name == "VerifyException")
                    {
                        verificationErrors.Add(e.Message);
                    }
                    else
                    {
                        _logger.LogError(e, "Error encountered");
                        throw;
                    }
                }
            }

            if (verificationErrors.Any())
            {
                string doubleNewLine = Environment.NewLine + Environment.NewLine;
                throw new TemplateVerificationException(
                    "Verification Failed." + doubleNewLine + string.Join(doubleNewLine, verificationErrors),
                    TemplateVerificationErrorCode.VerificationFailed);
            }
        }

        private AsyncLazy<string> GetVerificationContent(string filePath, CommandResult commandResult)
        {
            return new AsyncLazy<string>(async () =>
            {
                string content;
                if (SpecialFiles.IsSpecialFile(filePath))
                {
                    content = SpecialFiles.IsStdOut(filePath) ? commandResult.StdOut : commandResult.StdErr;
                }
                else
                {
                    content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                }

                if (_options.CustomScrubber != null)
                {
                    StringBuilder sb = new StringBuilder(content);
                    _options.CustomScrubber(filePath, sb);
                    content = sb.ToString();
                }

                return content;
            });
        }

        private static class SpecialFiles
        {
            public static readonly string[] FileNames = { StdOut, StdErr };

            private const string StdOut = "StdOut";
            private const string StdErr = "StdErr";

            public static bool IsSpecialFile(string filePath)
            {
                return FileNames.Contains(filePath, StringComparer.OrdinalIgnoreCase);
            }

            public static bool IsStdOut(string filePath) => filePath.Equals(StdOut, StringComparison.OrdinalIgnoreCase);

            public static bool IsStdErr(string filePath) => filePath.Equals(StdErr, StringComparison.OrdinalIgnoreCase);
        }

        private class LoggerProxy : ITestOutputHelper
        {
            private readonly ILogger _logger;

            public LoggerProxy(ILogger logger) => _logger = logger;

            public void WriteLine(string message) => _logger.LogInformation(message);

            public void WriteLine(string format, params object[] args) => _logger.LogInformation(format, args);
        }
    }
}
