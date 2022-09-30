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
        private readonly ILoggerFactory? _loggerFactory;

        static VerificationEngine()
        {
            // Customize diff output of verifier
            VerifyDiffPlex.Initialize(OutputType.Compact);
            VerifierSettings.UseSplitModeForUniqueDirectory();
        }

        public VerificationEngine(IOptions<TemplateVerifierOptions> optionsAccessor, ILogger logger)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _options = optionsAccessor.Value;
            _logger = logger;
        }

        public VerificationEngine(TemplateVerifierOptions options, ILoggerFactory loggerFactory)
        : this(options, loggerFactory.CreateLogger(typeof(VerificationEngine)))
        {
            _loggerFactory = loggerFactory;
        }

        public async Task Execute(CancellationToken cancellationToken = default)
        {
            //TODO: add functionality for uninstalled templates from a local folder
            if (!string.IsNullOrEmpty(_options.TemplatePath))
            {
                throw new TemplateVerificationException("Custom template path not yet supported.", TemplateVerificationErrorCode.InternalError);
            }

            CommandResult commandResult = RunDotnetNewCommand(_options, _loggerFactory, _logger);

            if (_options.IsCommandExpectedToFail ?? false)
            {
                if (commandResult.ExitCode == 0)
                {
                    throw new TemplateVerificationException(
                        LocalizableStrings.VerificationEngine_Error_UnexpectedPass,
                        TemplateVerificationErrorCode.VerificationFailed);
                }
            }
            else
            {
                if (commandResult.ExitCode != 0)
                {
                    throw new TemplateVerificationException(
                        string.Format(LocalizableStrings.VerificationEngine_Error_UnexpectedFail, commandResult.ExitCode),
                        TemplateVerificationErrorCode.InstantiationFailed);
                }

                // We do not expect stderr in passing command.
                // However if verification of stdout and stderr is opted-in - we will let that verification validate the stderr content
                if (!(_options.VerifyCommandOutput ?? false) && !string.IsNullOrEmpty(commandResult.StdErr))
                {
                    throw new TemplateVerificationException(
                        string.Format(
                            LocalizableStrings.VerificationEngine_Error_UnexpectedStdErr,
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

        private static CommandResult RunDotnetNewCommand(TemplateVerifierOptions options, ILoggerFactory? loggerFactory, ILogger logger)
        {
            //TODO: add functionality for uninstalled templates from a local folder
            if (!string.IsNullOrEmpty(options.TemplatePath))
            {
                throw new TemplateVerificationException("Custom template path not yet supported.", TemplateVerificationErrorCode.InternalError);
            }

            // Create temp folder and instantiate there
            string workingDir = options.OutputDirectory ?? Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            if (Directory.Exists(workingDir) && Directory.EnumerateFileSystemEntries(workingDir).Any())
            {
                throw new TemplateVerificationException(LocalizableStrings.VerificationEngine_Error_WorkDirExists, TemplateVerificationErrorCode.WorkingDirectoryExists);
            }

            Directory.CreateDirectory(workingDir);

            List<string> cmdArgs = new();
            if (!string.IsNullOrEmpty(options.DotnetNewCommandAssemblyPath))
            {
                cmdArgs.Add(options.DotnetNewCommandAssemblyPath);
            }
            cmdArgs.Add("new");
            cmdArgs.Add(options.TemplateName);
            if (options.TemplateSpecificArgs != null)
            {
                cmdArgs.AddRange(options.TemplateSpecificArgs);
            }
            cmdArgs.Add("--debug:ephemeral-hive");
            // let's make sure the template outputs are named deterministically
            cmdArgs.Add("-n");
            cmdArgs.Add(options.TemplateName);

            // TODO: export and use impl from sdk
            return new DotnetCommand(loggerFactory?.CreateLogger(typeof(DotnetCommand)) ?? logger, "dotnet", cmdArgs.ToArray())
                    .WithWorkingDirectory(workingDir)
                    .Execute();
        }

        private static Task CreateVerificationTask(CommandResult commandResult, TemplateVerifierOptions options)
        {
            string contentDir = commandResult.StartInfo.WorkingDirectory;

            List<string> exclusionsList = (options.DisableDefaultVerificationExcludePatterns ?? false)
                ? new()
                : new(_defaultVerificationExcludePatterns);

            if (options.VerificationExcludePatterns != null)
            {
                exclusionsList.AddRange(options.VerificationExcludePatterns);
            }

            List<Glob> globs = exclusionsList.Select(pattern => Glob.Parse(pattern)).ToList();

            if (options.CustomDirectoryVerifier != null)
            {
                return options.CustomDirectoryVerifier(
                    contentDir,
                    new Lazy<IAsyncEnumerable<(string FilePath, string ScrubbedContent)>>(
                        GetVerificationContent(contentDir, globs, options.CustomScrubbers)));
            }

            VerifySettings verifySettings = new();

            if (options.CustomScrubbers != null)
            {
                if (options.CustomScrubbers.GeneralScrubber != null)
                {
                    verifySettings.AddScrubber(options.CustomScrubbers.GeneralScrubber);
                }

                foreach (var pair in options.CustomScrubbers.ScrubersByExtension)
                {
                    verifySettings.AddScrubber(pair.Key, pair.Value);
                }
            }

            verifySettings.UseTypeName(options.TemplateName);
            verifySettings.UseDirectory(options.ExpectationsDirectory ?? "VerifyExpectations");
            verifySettings.UseMethodName(EncodeArgsAsPath(options.TemplateSpecificArgs));

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
                                verifySettings.UniqueForArchitecture();
                                break;
                            case UniqueForOption.OsPlatform:
                                verifySettings.UniqueForOSPlatform();
                                break;
                            case UniqueForOption.Runtime:
                                verifySettings.UniqueForRuntime();
                                break;
                            case UniqueForOption.RuntimeAndVersion:
                                verifySettings.UniqueForRuntimeAndVersion();
                                break;
                            case UniqueForOption.TargetFramework:
                                verifySettings.UniqueForTargetFramework();
                                break;
                            case UniqueForOption.TargetFrameworkAndVersion:
                                verifySettings.UniqueForTargetFrameworkAndVersion();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }

            if (options.DisableDiffTool ?? false)
            {
                verifySettings.DisableDiff();
            }

            return Verifier.VerifyDirectory(
                contentDir,
                (filePath) => !globs.Any(g => g.IsMatch(filePath)),
                settings: verifySettings);
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

        private async Task VerifyResult(TemplateVerifierOptions args, CommandResult commandResult)
        {
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
                            LocalizableStrings.VerificationEngine_Error_StdOutFolderExists,
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

            Task verifyTask = CreateVerificationTask(commandResult, args);

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
                    _logger.LogError(e, LocalizableStrings.VerificationEngine_Error_Unexpected);
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
    }
}
