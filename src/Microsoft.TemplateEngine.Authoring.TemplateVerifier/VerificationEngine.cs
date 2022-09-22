// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
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

        private static string EncodeArgsAsPath(IEnumerable<string>? args)
        {
            if (args == null || !args.Any())
            {
                return string.Empty;
            }

            Regex r = new Regex(string.Format("[{0}]", Regex.Escape(new string(Path.GetInvalidFileNameChars()))));
            return r.Replace(string.Join('#', args), string.Empty);
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

            if (_options.VerifyCommandOutput ?? false)
            {
                if (Directory.Exists(Path.Combine(commandResult.StartInfo.WorkingDirectory, SpecialFiles.StandardStreamsDir)))
                {
                    throw new TemplateVerificationException(
                        string.Format(
                            "Folder [{0}] not expected to exist in the template output - cannot verify stdout/stderr in such case",
                            SpecialFiles.StandardStreamsDir),
                        TemplateVerificationErrorCode.InternalError);
                }

                Directory.CreateDirectory(Path.Combine(commandResult.StartInfo.WorkingDirectory, SpecialFiles.StandardStreamsDir));

                await File.WriteAllTextAsync(
                    Path.Combine(commandResult.StartInfo.WorkingDirectory, SpecialFiles.StandardStreamsDir, SpecialFiles.StdOut),
                    commandResult.StdOut)
                    .ConfigureAwait(false);

                await File.WriteAllTextAsync(
                        Path.Combine(commandResult.StartInfo.WorkingDirectory, SpecialFiles.StandardStreamsDir, SpecialFiles.StdErr),
                        commandResult.StdErr)
                    .ConfigureAwait(false);
            }

            Verifier.DerivePathInfo(
                (sourceFile, projectDirectory, type, method) => new(
                    directory: args.ExpectationsDirectory ?? "VerifyExpectations",
                    typeName: args.TemplateName,
                    methodName: EncodeArgsAsPath(args.TemplateSpecificArgs)));

            try
            {
                SettingsTask defaultVerifyTask = Verifier.VerifyDirectory(
                    commandResult.StartInfo.WorkingDirectory,
                    (filePath) => !globs.Any(g => g.IsMatch(filePath)));

                if (_options.CustomScrubbers != null)
                {
                    if (_options.CustomScrubbers.GeneralScrubber != null)
                    {
                        defaultVerifyTask = defaultVerifyTask.AddScrubber(_options.CustomScrubbers.GeneralScrubber);
                    }

                    foreach (var pair in _options.CustomScrubbers.ScrubersByExtension)
                    {
                        defaultVerifyTask = defaultVerifyTask.AddScrubber(pair.Key, pair.Value);
                    }
                }

                if ((_options.UniqueFor ?? UniqueForOption.None) != UniqueForOption.None)
                {
                    foreach (UniqueForOption value in Enum.GetValues(typeof(UniqueForOption)))
                    {
                        if ((_options.UniqueFor & value) == value)
                        {
                            switch (value)
                            {
                                case UniqueForOption.None:
                                    break;
                                case UniqueForOption.Architecture:
                                    defaultVerifyTask = defaultVerifyTask.UniqueForArchitecture();
                                    break;
                                case UniqueForOption.OsPlatform:
                                    defaultVerifyTask = defaultVerifyTask.UniqueForOSPlatform();
                                    break;
                                case UniqueForOption.Runtime:
                                    defaultVerifyTask = defaultVerifyTask.UniqueForRuntime();
                                    break;
                                case UniqueForOption.RuntimeAndVersion:
                                    defaultVerifyTask = defaultVerifyTask.UniqueForRuntimeAndVersion();
                                    break;
                                case UniqueForOption.TargetFramework:
                                    defaultVerifyTask = defaultVerifyTask.UniqueForTargetFramework();
                                    break;
                                case UniqueForOption.TargetFrameworkAndVersion:
                                    defaultVerifyTask = defaultVerifyTask.UniqueForTargetFrameworkAndVersion();
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    }
                }

                if (_options.DisableDiffTool ?? false)
                {
                    defaultVerifyTask = defaultVerifyTask.DisableDiff();
                }

                Task verifyTask = _options.CustomVerifyDirectory != null
                    ? _options.CustomVerifyDirectory(commandResult.StartInfo.WorkingDirectory)
                    : defaultVerifyTask;

                await verifyTask.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (e is TemplateVerificationException)
                {
                    throw;
                }
                if (e.GetType().Name == "VerifyException")
                {
                    throw new TemplateVerificationException(e.Message, TemplateVerificationErrorCode.VerificationFailed);
                }
                else
                {
                    _logger.LogError(e, "Unexpected error encountered");
                    throw;
                }
            }
        }

        private static class SpecialFiles
        {
            public const string StandardStreamsDir = "std-streams";
            public const string StdOut = "stdout.txt";
            public const string StdErr = "stderr.txt";
            public static readonly string[] FileNames = { StdOut, StdErr };

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
