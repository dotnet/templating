// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Abstractions.TemplateFiltering;
using Microsoft.TemplateEngine.Cli.Alias;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Cli.HelpAndUsage;
using Microsoft.TemplateEngine.Cli.TemplateResolution;
using Microsoft.TemplateEngine.Cli.TemplateSearch;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Utils;
using TemplateCreator = Microsoft.TemplateEngine.Edge.Template.TemplateCreator;

namespace Microsoft.TemplateEngine.Cli
{
    public class New3Command
    {
        private static readonly Guid _entryMutexGuid = new Guid("5CB26FD1-32DB-4F4C-B3DC-49CFD61633D2");
        private static Mutex? _entryMutex;
        private readonly ITelemetryLogger _telemetryLogger;
        private readonly TemplateCreator _templateCreator;
        private readonly TemplatePackageManager _templatePackageManager;
        private readonly TemplateInformationCoordinator _templateInformationCoordinator;
        private readonly AliasRegistry _aliasRegistry;

        /// <summary>
        /// It's safe to access template agnostic information anytime after the first parse.
        /// But there is never a guarantee which template the parse is in the context of.
        /// </summary>
        private readonly INewCommandInput _commandInput;

        private readonly IHostSpecificDataLoader _hostDataLoader;
        private readonly string? _defaultLanguage;
        private readonly New3Callbacks _callbacks;
        private readonly Func<string> _inputGetter = () => Console.ReadLine() ?? string.Empty;

        internal New3Command(string commandName, ITemplateEngineHost host, ITelemetryLogger telemetryLogger, New3Callbacks callbacks, INewCommandInput commandInput)
            : this(commandName, host, telemetryLogger, callbacks, commandInput, null)
        {
        }

        internal New3Command(string commandName, ITemplateEngineHost host, ITelemetryLogger telemetryLogger, New3Callbacks callbacks, INewCommandInput commandInput, string? hivePath, bool virtualize = false)
        {
            _telemetryLogger = telemetryLogger;
            host = new CliTemplateEngineHost(host, this);
            EnvironmentSettings = new EngineEnvironmentSettings(host, settingsLocation: hivePath, virtualizeSettings: virtualize);
            _templatePackageManager = new TemplatePackageManager(EnvironmentSettings);
            _templateCreator = new TemplateCreator(EnvironmentSettings);
            _aliasRegistry = new AliasRegistry(EnvironmentSettings);
            CommandName = commandName;
            _hostDataLoader = new HostSpecificDataLoader(EnvironmentSettings);
            _commandInput = commandInput;
            _callbacks = callbacks ?? new New3Callbacks();

            if (!EnvironmentSettings.Host.TryGetHostParamDefault("prefs:language", out _defaultLanguage))
            {
                _defaultLanguage = null;
            }
            _templateInformationCoordinator = new TemplateInformationCoordinator(EnvironmentSettings, _templatePackageManager, _templateCreator, _hostDataLoader, _telemetryLogger, _defaultLanguage);
        }

        internal string TemplateName => _commandInput.TemplateName;

        internal string OutputPath => _commandInput.OutputPath;

        internal string CommandName { get; }

        internal EngineEnvironmentSettings EnvironmentSettings { get; private set; }

        /// <summary>
        /// Runs the command using <paramref name="host"/> and <paramref name="args"/>.
        /// </summary>
        /// <param name="commandName">Command name that is being executed.</param>
        /// <param name="host">The <see cref="ITemplateEngineHost"/> that executes the command.</param>
        /// <param name="telemetryLogger"><see cref="ITelemetryLogger"/> to use to track events.</param>
        /// <param name="callbacks">set of callbacks to be used, <see cref="New3Callbacks"/> for more details.</param>
        /// <param name="args">arguments to be run using template engine.</param>
        /// <param name="hivePath">(optional) the path to template engine settings to use.</param>
        /// <returns>exit code: 0 on success, other on error.</returns>
        /// <exception cref="CommandParserException">when <paramref name="args"/> cannot be parsed.</exception>
        public static int Run(string commandName, ITemplateEngineHost host, ITelemetryLogger telemetryLogger, New3Callbacks callbacks, string[] args, string? hivePath = null)
        {
            _ = host ?? throw new ArgumentNullException(nameof(host));
            _ = telemetryLogger ?? throw new ArgumentNullException(nameof(telemetryLogger));
            _ = callbacks ?? throw new ArgumentNullException(nameof(callbacks));
            _ = args ?? throw new ArgumentNullException(nameof(args));

            if (!args.Any(x => string.Equals(x, "--debug:ephemeral-hive")))
            {
                EnsureEntryMutex(hivePath, host);

                if (!_entryMutex!.WaitOne())
                {
                    return -1;
                }
            }

            try
            {
                return ActualRun(commandName, host, telemetryLogger, callbacks, args, hivePath);
            }
            finally
            {
                if (_entryMutex != null)
                {
                    _entryMutex.ReleaseMutex();
                }
            }
        }

        private static Mutex EnsureEntryMutex(string? hivePath, ITemplateEngineHost host)
        {
            if (_entryMutex == null)
            {
                string entryMutexIdentity;

                // this effectively mimics EngineEnvironmentSettings.BaseDir, which is not initialized when this is needed.
                if (!string.IsNullOrEmpty(hivePath))
                {
                    entryMutexIdentity = $"{_entryMutexGuid.ToString()}-{hivePath}".Replace("\\", "_").Replace("/", "_");
                }
                else
                {
                    entryMutexIdentity = $"{_entryMutexGuid.ToString()}-{host.HostIdentifier}-{host.Version}".Replace("\\", "_").Replace("/", "_");
                }

                _entryMutex = new Mutex(false, entryMutexIdentity);
            }

            return _entryMutex;
        }

        private static int ActualRun(string commandName, ITemplateEngineHost host, ITelemetryLogger telemetryLogger, New3Callbacks callbacks, string[] args, string? hivePath)
        {
            if (args.Any(x => string.Equals(x, "--debug:version", StringComparison.Ordinal)))
            {
                ShowVersion();
                return 0;
            }

            if (args.Any(x => string.Equals(x, "--debug:attach", StringComparison.Ordinal)))
            {
                Console.ReadLine();
            }

            int customHiveFlagIndex = args.ToList().IndexOf("--debug:custom-hive");
            if (customHiveFlagIndex >= 0)
            {
                if (customHiveFlagIndex + 1 >= args.Length)
                {
                    Reporter.Error.WriteLine("--debug:custom-hive requires 1 arg indicating the absolute or relative path to the custom hive".Bold().Red());
                    return 1;
                }

                hivePath = args[customHiveFlagIndex + 1];
                if (hivePath.StartsWith("-"))
                {
                    Reporter.Error.WriteLine("--debug:custom-hive requires 1 arg indicating the absolute or relative path to the custom hive".Bold().Red());
                    return 1;
                }

                hivePath = Path.GetFullPath(hivePath);
            }

            bool ephemeralHiveFlag = args.Any(x => string.Equals(x, "--debug:ephemeral-hive", StringComparison.Ordinal));

            if (args.Length == 0)
            {
                telemetryLogger.TrackEvent(commandName + TelemetryConstants.CalledWithNoArgsEventSuffix);
            }

            INewCommandInput commandInput = new NewCommandInputCli(commandName);
            New3Command instance = new New3Command(commandName, host, telemetryLogger, callbacks, commandInput, hivePath, virtualize: ephemeralHiveFlag);

            commandInput.OnExecute(instance.ExecuteAsync);

            int result;
            try
            {
                using (Timing.Over(instance.EnvironmentSettings.Host.Logger, "Execute"))
                {
                    result = commandInput.Execute(args);
                }
            }
            catch (Exception ex)
            {
                AggregateException? ax = ex as AggregateException;

                while (ax != null && ax.InnerExceptions.Count == 1 && ax.InnerException is not null)
                {
                    ex = ax.InnerException;
                    ax = ex as AggregateException;
                }

                Reporter.Error.WriteLine(ex.Message.Bold().Red());

                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    ax = ex as AggregateException;

                    while (ax != null && ax.InnerExceptions.Count == 1 && ax.InnerException is not null)
                    {
                        ex = ax.InnerException;
                        ax = ex as AggregateException;
                    }

                    Reporter.Error.WriteLine(ex.Message.Bold().Red());
                }

                Reporter.Error.WriteLine(ex.StackTrace.Bold().Red());
                result = 1;
            }
            finally
            {
                instance._templatePackageManager.Dispose();
            }

            return result;
        }

        private static void ShowVersion()
        {
            Reporter.Output.WriteLine(LocalizableStrings.CommandDescription);
            Reporter.Output.WriteLine();
            int targetLength = Math.Max(LocalizableStrings.Version.Length, LocalizableStrings.CommitHash.Length);
            Reporter.Output.WriteLine($" {LocalizableStrings.Version.PadRight(targetLength)} {GitInfo.PackageVersion}");
            Reporter.Output.WriteLine($" {LocalizableStrings.CommitHash.PadRight(targetLength)} {GitInfo.CommitHash}");
        }

        // TODO: make sure help / usage works right in these cases.
        private async Task<New3CommandStatus> EnterMaintenanceFlowAsync()
        {
            if (!TemplateResolver.ValidateRemainingParameters(_commandInput, out IReadOnlyList<string> invalidParams))
            {
                TemplateInformationCoordinator.DisplayInvalidParameters(invalidParams);
                if (_commandInput.IsHelpFlagSpecified)
                {
                    // this code path doesn't go through the full help & usage stack, so needs it's own call to ShowUsageHelp().
                    _templateInformationCoordinator.ShowUsageHelp(_commandInput);
                }
                else
                {
                    Reporter.Error.WriteLine(string.Format(LocalizableStrings.RunHelpForInformationAboutAcceptedParameters, CommandName).Bold().Red());
                }

                return New3CommandStatus.InvalidParamValues;
            }

            // dotnet new -h case
            if (_commandInput.IsHelpFlagSpecified)
            {
                _templateInformationCoordinator.ShowUsageHelp(_commandInput);
                return New3CommandStatus.Success;
            }

            // No other cases specified, we've fallen through to "Optional usage help + List"
            return await _templateInformationCoordinator.DisplayTemplateGroupListAsync(_commandInput, default).ConfigureAwait(false);
        }

        private async Task<New3CommandStatus> EnterTemplateManipulationFlowAsync()
        {
            if (_commandInput.IsHelpFlagSpecified)
            {
                return await _templateInformationCoordinator.DisplayDetailedHelpAsync(_commandInput, default).ConfigureAwait(false);
            }
            if (_commandInput.IsListFlagSpecified)
            {
                return await _templateInformationCoordinator.DisplayTemplateGroupListAsync(_commandInput, default).ConfigureAwait(false);
            }

            TemplateInvocationCoordinator invocationCoordinator = new TemplateInvocationCoordinator(
                EnvironmentSettings,
                _templatePackageManager,
                _templateInformationCoordinator,
                _hostDataLoader,
                _telemetryLogger,
                _defaultLanguage,
                _inputGetter,
                _callbacks);

            return await invocationCoordinator.CoordinateInvocationAsync(_commandInput, default).ConfigureAwait(false);
        }

        private async Task<New3CommandStatus> ExecuteAsync()
        {
            // this is checking the initial parse, which is template agnostic.
            if (_commandInput.HasParseError)
            {
                return _templateInformationCoordinator.HandleParseError(_commandInput);
            }

            if (_commandInput.IsHelpFlagSpecified)
            {
                _telemetryLogger.TrackEvent(CommandName + TelemetryConstants.HelpEventSuffix);
            }

            if (_commandInput.ShowAliasesSpecified)
            {
                return AliasSupport.DisplayAliasValues(EnvironmentSettings, _commandInput, _aliasRegistry, CommandName);
            }

            if (_commandInput.ExpandedExtraArgsFiles && string.IsNullOrEmpty(_commandInput.Alias))
            {
                // Only show this if there was no alias expansion.
                // ExpandedExtraArgsFiles must be checked before alias expansion - it'll get reset if there's an alias.
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.ExtraArgsCommandAfterExpansion, string.Join(" ", _commandInput.Tokens)));
            }

            if (string.IsNullOrEmpty(_commandInput.Alias))
            {
                // The --alias param is for creating / updating / deleting aliases.
                // If it's not present, try expanding aliases now.
                New3CommandStatus aliasExpansionResult = AliasSupport.CoordinateAliasExpansion(_commandInput, _aliasRegistry, _templateInformationCoordinator);

                if (aliasExpansionResult != New3CommandStatus.Success)
                {
                    return aliasExpansionResult;
                }
            }

            if (!Initialize())
            {
                return New3CommandStatus.Success;
            }

            bool forceCacheRebuild = _commandInput.HasDebuggingFlag("--debug:rebuildcache");
            try
            {
                if (forceCacheRebuild)
                {
                    await _templatePackageManager.RebuildTemplateCacheAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (EngineInitializationException eiex)
            {
                Reporter.Error.WriteLine(eiex.Message.Bold().Red());
                Reporter.Error.WriteLine(LocalizableStrings.SettingsReadError);
                return New3CommandStatus.CreateFailed;
            }

            try
            {
                if (!string.IsNullOrEmpty(_commandInput.Alias) && !_commandInput.IsHelpFlagSpecified)
                {
                    return AliasSupport.ManipulateAliasIfValid(_aliasRegistry, _commandInput.Alias, _commandInput.Tokens.ToList(), await GetAllTemplateShortNamesAsync().ConfigureAwait(false));
                }

                if (TemplatePackageCoordinator.IsTemplatePackageManipulationFlow(_commandInput))
                {
                    TemplatePackageCoordinator packageCoordinator = new TemplatePackageCoordinator(_telemetryLogger, EnvironmentSettings, _templatePackageManager, _templateInformationCoordinator, _defaultLanguage);
                    return await packageCoordinator.ProcessAsync(_commandInput).ConfigureAwait(false);
                }

                if (_commandInput.SearchOnline)
                {
                    return await CliTemplateSearchCoordinator.SearchForTemplateMatchesAsync(EnvironmentSettings, _templatePackageManager, _commandInput, _defaultLanguage).ConfigureAwait(false);
                }

                if (string.IsNullOrWhiteSpace(TemplateName))
                {
                    return await EnterMaintenanceFlowAsync().ConfigureAwait(false);
                }

                return await EnterTemplateManipulationFlowAsync().ConfigureAwait(false);
            }
            catch (TemplateAuthoringException tae)
            {
                Reporter.Error.WriteLine(tae.Message.Bold().Red());
                return New3CommandStatus.CreateFailed;
            }
        }

        private bool Initialize()
        {
            bool reinitFlag = _commandInput.HasDebuggingFlag("--debug:reinit");
            if (reinitFlag)
            {
                EnvironmentSettings.Host.FileSystem.DirectoryDelete(EnvironmentSettings.Paths.HostVersionSettingsDir, true);
            }

            if (_commandInput.HasDebuggingFlag("--debug:showconfig"))
            {
                ShowConfig();
                return false;
            }

            return true;
        }

        private async Task<HashSet<string>> GetAllTemplateShortNamesAsync()
        {
            IReadOnlyCollection<ITemplateMatchInfo> allTemplates = TemplateResolver.PerformAllTemplatesQuery(await _templatePackageManager.GetTemplatesAsync(default).ConfigureAwait(false), _hostDataLoader);

            HashSet<string> allShortNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (ITemplateMatchInfo templateMatchInfo in allTemplates)
            {
                allShortNames.UnionWith(templateMatchInfo.Info.ShortNameList);
            }

            return allShortNames;
        }

        private void ShowConfig()
        {
            Reporter.Output.WriteLine(LocalizableStrings.CurrentConfiguration);
            Reporter.Output.WriteLine(" ");

            TableFormatter.Print(EnvironmentSettings.Components.OfType<IMountPointFactory>(), LocalizableStrings.NoItems, "   ", '-', new Dictionary<string, Func<IMountPointFactory, object>>
            {
                { LocalizableStrings.MountPointFactories, x => x.Id },
                { LocalizableStrings.Type, x => x.GetType().FullName ?? string.Empty },
                { LocalizableStrings.Assembly, x => x.GetType().GetTypeInfo().Assembly.FullName ?? string.Empty }
            });

            TableFormatter.Print(EnvironmentSettings.Components.OfType<IGenerator>(), LocalizableStrings.NoItems, "   ", '-', new Dictionary<string, Func<IGenerator, object>>
            {
                { LocalizableStrings.Generators, x => x.Id },
                { LocalizableStrings.Type, x => x.GetType().FullName ?? string.Empty },
                { LocalizableStrings.Assembly, x => x.GetType().GetTypeInfo().Assembly.FullName ?? string.Empty }
            });
        }
    }
}
