// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.TemplateEngine.Authoring.TemplateVerifier.Commands;
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
        private readonly ILogger _logger;

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
            //TODO: add functionality for uninstalled templates from a local folder
            if (!string.IsNullOrEmpty(_options.TemplatePath))
            {
                throw new TemplateVerificationException("Custom template path not yet supported.", TemplateVerificationErrorCode.InternalError);
            }

            if (string.IsNullOrEmpty(_options.TemplateName))
            {
                throw new TemplateVerificationException(LocalizableStrings.engine_error_templateNameMandatory, TemplateVerificationErrorCode.InternalError);
            }

            // Create temp folder and instantiate there
            string workingDir = _options.OutputDirectory ?? Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            if (Directory.Exists(workingDir) && Directory.EnumerateFileSystemEntries(workingDir).Any())
            {
                throw new TemplateVerificationException(LocalizableStrings.engine_error_workDirExists, TemplateVerificationErrorCode.WorkingDirectoryExists);
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
                if (commandResult.ExitCode == 0)
                {
                    throw new TemplateVerificationException(
                        LocalizableStrings.engine_error_unexpectedPass,
                        TemplateVerificationErrorCode.VerificationFailed);
                }
            }
            else
            {
                if (commandResult.ExitCode != 0)
                {
                    throw new TemplateVerificationException(
                        string.Format(LocalizableStrings.engine_error_unexpectedFail, commandResult.ExitCode),
                        TemplateVerificationErrorCode.InstantiationFailed);
                }

                // We do not expect stderr in passing command.
                // However if verification of stdout and stderr is opted-in - we will let that verification validate the stderr content
                if (!(_options.VerifyCommandOutput ?? false) && !string.IsNullOrEmpty(commandResult.StdErr))
                {
                    throw new TemplateVerificationException(
                        string.Format(
                            LocalizableStrings.engine_error_unexpectedStdErr,
                            Environment.NewLine,
                            commandResult.StdErr),
                        TemplateVerificationErrorCode.InstantiationFailed);
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

        private static async IAsyncEnumerable<(string FilePath, string ScrubbedContent)> GetVerificationContent(string contentDir, List<Glob> globs, ScrubbersDefinition? scrubbers)
        {
            foreach (string filePath in Directory.EnumerateFiles(contentDir, "*", SearchOption.AllDirectories))
            {
                if (globs.Any(g => g.IsMatch(filePath)))
                {
                    continue;
                }

                string content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

                if (scrubbers != null)
                {
                    string extension = Path.GetExtension(filePath);

                    if (string.IsNullOrEmpty(extension) || !scrubbers.ScrubersByExtension.TryGetValue(extension, out Action<StringBuilder>? scrubber))
                    {
                        scrubber = scrubbers.GeneralScrubber;
                    }

                    if (scrubber != null)
                    {
                        var sb = new StringBuilder(content);
                        scrubber(sb);
                        content = sb.ToString();
                    }
                }

                yield return new(filePath, content);
            }
        }

        private static Task CreateVerificationTask(string contentDir, TemplateVerifierOptions options)
        {
            List<string> exclusionsList = (options.DisableDefaultVerificationExcludePatterns ?? false)
                ? new()
                : new(_defaultVerificationExcludePatterns);

            if (options.VerificationExcludePatterns != null)
            {
                exclusionsList.AddRange(options.VerificationExcludePatterns);
            }

            List<Glob> globs = exclusionsList.Select(pattern => Glob.Parse(pattern)).ToList();

            SettingsTask defaultVerifyTask = Verifier.VerifyDirectory(
                contentDir,
                (filePath) => !globs.Any(g => g.IsMatch(filePath)));

            if (options.CustomScrubbers != null)
            {
                if (options.CustomScrubbers.GeneralScrubber != null)
                {
                    defaultVerifyTask = defaultVerifyTask.AddScrubber(options.CustomScrubbers.GeneralScrubber);
                }

                foreach (var pair in options.CustomScrubbers.ScrubersByExtension)
                {
                    defaultVerifyTask = defaultVerifyTask.AddScrubber(pair.Key, pair.Value);
                }
            }

            if (options.CustomDirectoryVerifier != null)
            {
                return options.CustomDirectoryVerifier(
                    contentDir,
                    new Lazy<IAsyncEnumerable<(string FilePath, string ScrubbedContent)>>(
                        GetVerificationContent(contentDir, globs, options.CustomScrubbers)));
            }

            Verifier.DerivePathInfo(
                (sourceFile, projectDirectory, type, method) => new(
                    directory: options.ExpectationsDirectory ?? "VerifyExpectations",
                    typeName: options.TemplateName,
                    methodName: EncodeArgsAsPath(options.TemplateSpecificArgs)));

            if ((options.UniqueFor ?? UniqueForOption.None) != UniqueForOption.None)
            {
                foreach (UniqueForOption value in Enum.GetValues(typeof(UniqueForOption)))
                {
                    if ((options.UniqueFor & value) == value)
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

            if (options.DisableDiffTool ?? false)
            {
                defaultVerifyTask = defaultVerifyTask.DisableDiff();
            }

            return defaultVerifyTask;
        }

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

            if (_options.VerifyCommandOutput ?? false)
            {
                if (Directory.Exists(Path.Combine(commandResult.StartInfo.WorkingDirectory, SpecialFiles.StandardStreamsDir)))
                {
                    throw new TemplateVerificationException(
                        string.Format(
                            LocalizableStrings.engine_error_stdOutFolderExists,
                            SpecialFiles.StandardStreamsDir),
                        TemplateVerificationErrorCode.InternalError);
                }

                Directory.CreateDirectory(Path.Combine(commandResult.StartInfo.WorkingDirectory, SpecialFiles.StandardStreamsDir));

                await File.WriteAllTextAsync(
                    Path.Combine(commandResult.StartInfo.WorkingDirectory, SpecialFiles.StandardStreamsDir, SpecialFiles.StdOut + (_options.StandardOutputFileExtension ?? SpecialFiles.DefaultExtension)),
                    commandResult.StdOut)
                    .ConfigureAwait(false);

                await File.WriteAllTextAsync(
                        Path.Combine(commandResult.StartInfo.WorkingDirectory, SpecialFiles.StandardStreamsDir, SpecialFiles.StdErr + (_options.StandardOutputFileExtension ?? SpecialFiles.DefaultExtension)),
                        commandResult.StdErr)
                    .ConfigureAwait(false);
            }

            Task verifyTask = CreateVerificationTask(commandResult.StartInfo.WorkingDirectory, args);

            try
            {
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
                    _logger.LogError(e, LocalizableStrings.engine_error_unexpected);
                    throw;
                }
            }
        }

        private static class SpecialFiles
        {
            public const string StandardStreamsDir = "std-streams";
            public const string StdOut = "stdout";
            public const string StdErr = "stderr";
            public const string DefaultExtension = ".txt";
            public static readonly string[] FileNames = { StdOut, StdErr };
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
