// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.TemplateLocator;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Cli.HelpAndUsage;
using Microsoft.TemplateEngine.Cli.NuGet;
using Microsoft.TemplateEngine.Cli.TemplateResolution;
using Microsoft.TemplateEngine.Cli.TemplateSearch;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Utils;
using NuGet.Credentials;

namespace Microsoft.TemplateEngine.Cli
{
    public class New3Command
    {
        private readonly ITelemetryLogger _telemetryLogger;
        private readonly TemplateCreator _templateCreator;
        private readonly SettingsLoader _settingsLoader;
        private readonly AliasRegistry _aliasRegistry;
        private readonly Paths _paths;
        private readonly INewCommandInput _commandInput;    // It's safe to access template agnostic information anytime after the first parse. But there is never a guarantee which template the parse is in the context of.
        private readonly IHostSpecificDataLoader _hostDataLoader;
        private readonly string _defaultLanguage;
        private static readonly Regex LocaleFormatRegex = new Regex(@"
                    ^
                        [a-z]{2}
                        (?:-[A-Z]{2})?
                    $",
            RegexOptions.IgnorePatternWhitespace);
        private readonly New3Callbacks _callbacks;
        private readonly Func<string> _inputGetter = () => Console.ReadLine();

        public New3Command(string commandName, ITemplateEngineHost host, ITelemetryLogger telemetryLogger, New3Callbacks callbacks, INewCommandInput commandInput)
            : this(commandName, host, telemetryLogger, callbacks, commandInput, null)
        {
        }

        public New3Command(string commandName, ITemplateEngineHost host, ITelemetryLogger telemetryLogger, New3Callbacks callbacks, INewCommandInput commandInput, string hivePath)
        {
            _telemetryLogger = telemetryLogger;
            host = new ExtendedTemplateEngineHost(host, this);
            EnvironmentSettings = new EngineEnvironmentSettings(host, x => new SettingsLoader(x), hivePath);
            _settingsLoader = (SettingsLoader)EnvironmentSettings.SettingsLoader;
            _templateCreator = new TemplateCreator(EnvironmentSettings);
            _aliasRegistry = new AliasRegistry(EnvironmentSettings);
            CommandName = commandName;
            _paths = new Paths(EnvironmentSettings);
            _hostDataLoader = new HostSpecificDataLoader(EnvironmentSettings.SettingsLoader);
            _commandInput = commandInput;
            _callbacks = callbacks;
            if (callbacks == null)
            {
                callbacks = new New3Callbacks();
            }

            if (!EnvironmentSettings.Host.TryGetHostParamDefault("prefs:language", out _defaultLanguage))
            {
                _defaultLanguage = null;
            }
        }

        public string CommandName { get; }

        public string TemplateName => _commandInput.TemplateName;

        public string OutputPath => _commandInput.OutputPath;

        public EngineEnvironmentSettings EnvironmentSettings { get; private set; }

        public static int Run(string commandName, ITemplateEngineHost host, ITelemetryLogger telemetryLogger, Action<IEngineEnvironmentSettings> onFirstRun, string[] args)
        {
            return Run(commandName, host, telemetryLogger, new New3Callbacks() { OnFirstRun = onFirstRun }, args, null);
        }

        private static readonly Guid _entryMutexGuid = new Guid("5CB26FD1-32DB-4F4C-B3DC-49CFD61633D2");
        private static Mutex _entryMutex;

        private static Mutex EnsureEntryMutex(string hivePath, ITemplateEngineHost host)
        {
            if (_entryMutex == null)
            {
                string _entryMutexIdentity;

                // this effectively mimics EngineEnvironmentSettings.BaseDir, which is not initialized when this is needed.
                if (!string.IsNullOrEmpty(hivePath))
                {
                    _entryMutexIdentity = $"{_entryMutexGuid.ToString()}-{hivePath}".Replace("\\", "_").Replace("/", "_");
                }
                else
                {
                    _entryMutexIdentity = $"{_entryMutexGuid.ToString()}-{host.HostIdentifier}-{host.Version}".Replace("\\", "_").Replace("/", "_");
                }

                _entryMutex = new Mutex(false, _entryMutexIdentity);
            }

            return _entryMutex;
        }

        public static int Run(string commandName, ITemplateEngineHost host, ITelemetryLogger telemetryLogger, Action<IEngineEnvironmentSettings> onFirstRun, string[] args, string hivePath)
        {
            return Run(commandName, host, telemetryLogger, new New3Callbacks() { OnFirstRun = onFirstRun }, args, hivePath);
        }

        public static int Run(string commandName, ITemplateEngineHost host, ITelemetryLogger telemetryLogger, New3Callbacks callbacks, string[] args, string hivePath)
        {
            if (!args.Any(x => string.Equals(x, "--debug:ephemeral-hive")))
            {
                EnsureEntryMutex(hivePath, host);

                if (!_entryMutex.WaitOne())
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

        private static int ActualRun(string commandName, ITemplateEngineHost host, ITelemetryLogger telemetryLogger, New3Callbacks callbacks, string[] args, string hivePath)
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
            }

            if (args.Length == 0)
            {
                telemetryLogger.TrackEvent(commandName + TelemetryConstants.CalledWithNoArgsEventSuffix);
            }

            INewCommandInput commandInput = new NewCommandInputCli(commandName);
            New3Command instance = new New3Command(commandName, host, telemetryLogger, callbacks, commandInput, hivePath);

            commandInput.OnExecute(instance.ExecuteAsync);

            int result;
            try
            {
                using (Timing.Over(host, "Execute"))
                {
                    result = commandInput.Execute(args);
                }
            }
            catch (Exception ex)
            {
                AggregateException ax = ex as AggregateException;

                while (ax != null && ax.InnerExceptions.Count == 1)
                {
                    ex = ax.InnerException;
                    ax = ex as AggregateException;
                }

                Reporter.Error.WriteLine(ex.Message.Bold().Red());

                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    ax = ex as AggregateException;

                    while (ax != null && ax.InnerExceptions.Count == 1)
                    {
                        ex = ax.InnerException;
                        ax = ex as AggregateException;
                    }

                    Reporter.Error.WriteLine(ex.Message.Bold().Red());
                }

                Reporter.Error.WriteLine(ex.StackTrace.Bold().Red());
                result = 1;
            }

            return result;
        }

        private void ConfigureEnvironment()
        {
            // delete everything from previous attempts for this install when doing first run setup.
            // don't want to leave partial setup if it's in a bad state.
            if (_paths.Exists(_paths.User.BaseDir))
            {
                _paths.DeleteDirectory(_paths.User.BaseDir);
            }

            _callbacks.OnFirstRun?.Invoke(EnvironmentSettings);
            EnvironmentSettings.SettingsLoader.Components.RegisterMany(typeof(New3Command).GetTypeInfo().Assembly.GetTypes());
        }

        private async Task<CreationResultStatus> EnterInstallFlowAsync()
        {
            CreationResultStatus resultStatus = CreationResultStatus.Success;
            _telemetryLogger.TrackEvent(CommandName + TelemetryConstants.InstallEventSuffix, new Dictionary<string, string> { { TelemetryConstants.ToInstallCount, _commandInput.ToInstallList.Count.ToString() } });

            var details = new Dictionary<string, string>();
            if (_commandInput.InstallNuGetSourceList?.Count > 0)
            {
                details[InstallerConstants.NuGetSourcesKey] = string.Join(InstallerConstants.NuGetSourcesSeparator.ToString(), _commandInput.InstallNuGetSourceList);
                DefaultCredentialServiceUtility.SetupDefaultCredentialService(new CliNuGetLogger(), !_commandInput.IsInteractiveFlagSpecified);
            }
            if (_commandInput.IsInteractiveFlagSpecified)
            {
                details[InstallerConstants.InteractiveModeKey] = "true";
            }

            // In future we might want give user ability to pick IManagerSourceProvider by Name or GUID
            var managedSourceProvider = EnvironmentSettings.SettingsLoader.TemplatesSourcesManager.GetManagedProvider(GlobalSettingsTemplatesSourcesProviderFactory.FactoryId);
            List<InstallRequest> installRequests = new List<InstallRequest>();
            foreach (string unexpandedInstallRequest in _commandInput.ToInstallList)
            {
                foreach (var expandedInstallRequest in InstallRequestPathResolution.Expand(unexpandedInstallRequest, EnvironmentSettings))
                {
                    var splitByColons = expandedInstallRequest.Split(new[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
                    string identifier = splitByColons[0];
                    string version = null;
                    if (splitByColons.Length > 1)
                    {
                        version = splitByColons[1];
                    }

                    installRequests.Add(new InstallRequest()
                        {
                            Identifier = identifier,
                            Version = version,
                            Details = details,
                            //TODO: Not needed, for now, but in future when we have more installers then just NuGet and Folder
                            //give user ability to set InstallerName
                            //InstallerName = _commandInput.InstallerName,
                        });
                }
            }

            if (!installRequests.Any())
            {
                Reporter.Error.WriteLine($"Found no template packages to install");
                return CreationResultStatus.NotFound;
            }

            Reporter.Output.WriteLine("The following template packages will be installed:");
            foreach (InstallRequest installRequest in installRequests)
            {
                if (string.IsNullOrWhiteSpace(installRequest.Version))
                {
                    Reporter.Output.WriteLine($"  {installRequest.Identifier}");
                }
                else
                {
                    Reporter.Output.WriteLine($"  {installRequest.Identifier}, version: {installRequest.Version}");
                }
            }
            Reporter.Output.WriteLine();

            var installResults = await managedSourceProvider.InstallAsync(installRequests, CancellationToken.None).ConfigureAwait(false);
            foreach (InstallResult result in installResults)
            {
                await DisplayInstallResultAsync(result.InstallRequest.DisplayName, result);
                if (!result.Success)
                {
                    resultStatus = CreationResultStatus.CreateFailed;
                }
            }
            return resultStatus;
        }

        private async Task DisplayInstallResultAsync(string packageToInstall, Result result)
        {
            if (result.Success)
            {
                Reporter.Output.WriteLine($"Success: {result.Source.DisplayName} installed the following templates:");

                IEnumerable<ITemplateInfo> templates = await result.Source.GetTemplates(EnvironmentSettings).ConfigureAwait(false);
                HelpForTemplateResolution.DisplayTemplateList(templates, EnvironmentSettings, _commandInput, _defaultLanguage);
            }
            else
            {
                switch (result.Error)
                {
                    case InstallerErrorCode.InvalidSource:
                        Reporter.Error.WriteLine(string.Format(LocalizableStrings.InstallFailedInvalidSource, packageToInstall, result.ErrorMessage).Bold().Red());
                        break;
                    case InstallerErrorCode.PackageNotFound:
                        Reporter.Error.WriteLine(string.Format(LocalizableStrings.InstallFailedPackageNotFound, packageToInstall).Bold().Red());
                        break;
                    case InstallerErrorCode.DownloadFailed:
                        Reporter.Error.WriteLine(string.Format(LocalizableStrings.InstallFailedDownloadFailed, packageToInstall).Bold().Red());
                        break;
                    case InstallerErrorCode.UnsupportedRequest:
                        Reporter.Error.WriteLine(string.Format(LocalizableStrings.InstallFailedUnsupportedRequest, packageToInstall).Bold().Red());
                        break;
                    case InstallerErrorCode.GenericError:
                        Reporter.Error.WriteLine(string.Format(LocalizableStrings.InstallFailedGenericError, packageToInstall).Bold().Red());
                        break;
                    case InstallerErrorCode.AlreadyInstalled:
                        Reporter.Error.WriteLine($"{packageToInstall} is already installed.".Bold().Red());
                        break;
                    case InstallerErrorCode.UpdateUninstallFailed:
                        Reporter.Error.WriteLine($"Failed to install {packageToInstall}, failed to uninstall previous version of the template package.".Bold().Red());
                        break;
                    case InstallerErrorCode.InvalidPackage:
                        Reporter.Error.WriteLine($"Failed to install {packageToInstall}, the template package is invalid.".Bold().Red());
                        break;
                }
            }
        }

        // TODO: make sure help / usage works right in these cases.
        private async Task<CreationResultStatus> EnterMaintenanceFlow()
        {
            if (!TemplateResolver.ValidateRemainingParameters(_commandInput, out IReadOnlyList<string> invalidParams))
            {
                HelpForTemplateResolution.DisplayInvalidParameters(invalidParams);
                if (_commandInput.IsHelpFlagSpecified)
                {
                    // this code path doesn't go through the full help & usage stack, so needs it's own call to ShowUsageHelp().
                    HelpForTemplateResolution.ShowUsageHelp(_commandInput, _telemetryLogger);
                }
                else
                {
                    Reporter.Error.WriteLine(string.Format(LocalizableStrings.RunHelpForInformationAboutAcceptedParameters, CommandName).Bold().Red());
                }

                return CreationResultStatus.InvalidParamValues;
            }

            if (_commandInput.ToUninstallList != null)
            {
                return await EnterUninstallFlowAsync().ConfigureAwait(false);
            }

            if (_commandInput.ToInstallList != null && _commandInput.ToInstallList.Count > 0 && _commandInput.ToInstallList[0] != null)
            {
                return await EnterInstallFlowAsync().ConfigureAwait(false);
            }

            // No other cases specified, we've fallen through to "Optional usage help + List"
            TemplateListResolutionResult templateResolutionResult = TemplateResolver.GetTemplateResolutionResultForListOrHelp(await _settingsLoader.GetTemplatesAsync(default), _hostDataLoader, _commandInput, _defaultLanguage);
            HelpForTemplateResolution.CoordinateHelpAndUsageDisplay(templateResolutionResult, EnvironmentSettings, _commandInput, _hostDataLoader, _telemetryLogger, _templateCreator, _defaultLanguage, showUsageHelp: _commandInput.IsHelpFlagSpecified);

            return CreationResultStatus.Success;
        }

        private async Task<CreationResultStatus> EnterUninstallFlowAsync()
        {
            CreationResultStatus result = CreationResultStatus.Success;
            if (_commandInput.ToUninstallList.Count > 0 && _commandInput.ToUninstallList[0] != null)
            {
                var managedSourcedGroupedByProvider = await EnvironmentSettings.SettingsLoader.TemplatesSourcesManager.GetManagedSourcesGroupedByProvider().ConfigureAwait(false);
                bool sourceToUninstallFound = false;
                foreach (var (provider, sources) in managedSourcedGroupedByProvider)
                {
                    var sourcesToUninstall = sources.Where(source => _commandInput.ToUninstallList.Contains(source.Identifier, StringComparer.OrdinalIgnoreCase));
                    if (!sourcesToUninstall.Any())
                    {
                        continue;
                    }
                    sourceToUninstallFound = true;
                    var uninstallResults = await provider.UninstallAsync(sourcesToUninstall, CancellationToken.None).ConfigureAwait(false);
                    foreach (UninstallResult uninstallResult in uninstallResults)
                    {
                        if (uninstallResult.Success)
                        {
                            Reporter.Output.WriteLine($"Success: {uninstallResult.Source.DisplayName} was uninstalled.");
                        }
                        else
                        {
                            Reporter.Error.WriteLine(string.Format(LocalizableStrings.CouldntUninstall, uninstallResult.Source.DisplayName, uninstallResult.ErrorMessage));
                            result = CreationResultStatus.CreateFailed;
                        }
                    }
                }
                if (!sourceToUninstallFound)
                {
                    Reporter.Error.WriteLine("The template package is not found, check installed sources and the uninstallation commands from the list below.");
                    result = CreationResultStatus.NotFound;
                    await DisplayInstalledTemplatesSources().ConfigureAwait(false);
                }
            }
            else
            {
                await DisplayInstalledTemplatesSources().ConfigureAwait(false);
            }

            return result;
        }

        private async Task DisplayInstalledTemplatesSources()
        {
            IEnumerable<IManagedTemplatesSource> managedTemplatesSources = await EnvironmentSettings.SettingsLoader.TemplatesSourcesManager.GetManagedTemplatesSources().ConfigureAwait(false);

            Reporter.Output.WriteLine(LocalizableStrings.InstalledItems);

            if (!managedTemplatesSources.Any())
            {
                Reporter.Output.WriteLine(LocalizableStrings.NoItems);
                return;
            }

            foreach (IManagedTemplatesSource managedSource in managedTemplatesSources)
            {
                Reporter.Output.WriteLine($"  {managedSource.Identifier}");
                if (!string.IsNullOrWhiteSpace(managedSource.Version))
                {
                    Reporter.Output.WriteLine($"    {LocalizableStrings.Version} {managedSource.Version}");
                }

                IReadOnlyDictionary<string, string> displayDetails = managedSource.GetDisplayDetails();
                //TODO: localize keys
                if (displayDetails?.Any() ?? false)
                {
                    Reporter.Output.WriteLine($"    {LocalizableStrings.UninstallListDetailsHeader}");
                    foreach (KeyValuePair<string, string> detail in displayDetails)
                    {
                        Reporter.Output.WriteLine($"      {detail.Key}: {detail.Value}");
                    }
                }

                IEnumerable<ITemplateInfo> templates = await managedSource.GetTemplates(EnvironmentSettings);
                if (templates.Any())
                {
                    Reporter.Output.WriteLine($"    {LocalizableStrings.Templates}:");
                    foreach (TemplateInfo info in templates)
                    {
                        string templateLanguage = info.GetLanguage();
                        if (!string.IsNullOrWhiteSpace(templateLanguage))
                        {
                            Reporter.Output.WriteLine($"      {info.Name} ({info.ShortName}) {templateLanguage}");
                        }
                        else
                        {
                            Reporter.Output.WriteLine($"      {info.Name} ({info.ShortName})");
                        }
                    }
                }

                // uninstall command:
                Reporter.Output.WriteLine($"    {LocalizableStrings.UninstallListUninstallCommand}");
                Reporter.Output.WriteLine(string.Format("      dotnet {0} -u {1}", _commandInput.CommandName, managedSource.Identifier));

                Reporter.Output.WriteLine();
            }
        }

        private async Task<CreationResultStatus> EnterTemplateManipulationFlowAsync()
        {
            if (_commandInput.IsListFlagSpecified || _commandInput.IsHelpFlagSpecified)
            {
                TemplateListResolutionResult listingTemplateResolutionResult = TemplateResolver.GetTemplateResolutionResultForListOrHelp(await _settingsLoader.GetTemplatesAsync(default), _hostDataLoader, _commandInput, _defaultLanguage);
                return HelpForTemplateResolution.CoordinateHelpAndUsageDisplay(listingTemplateResolutionResult, EnvironmentSettings, _commandInput, _hostDataLoader, _telemetryLogger, _templateCreator, _defaultLanguage, showUsageHelp: _commandInput.IsHelpFlagSpecified);
            }

            TemplateResolutionResult templateResolutionResult = TemplateResolver.GetTemplateResolutionResult(await _settingsLoader.GetTemplatesAsync(default), _hostDataLoader, _commandInput, _defaultLanguage);
            if (templateResolutionResult.ResolutionStatus == TemplateResolutionResult.Status.SingleMatch)
            {
                TemplateInvocationCoordinator invocationCoordinator = new TemplateInvocationCoordinator(_settingsLoader, _commandInput, _telemetryLogger, CommandName, _inputGetter, _callbacks);
                return await invocationCoordinator.CoordinateInvocationOrAcquisitionAsync(templateResolutionResult.TemplateToInvoke, CancellationToken.None);
            }
            else
            {
                return await HelpForTemplateResolution.CoordinateAmbiguousTemplateResolutionDisplay(templateResolutionResult, EnvironmentSettings, _commandInput, _defaultLanguage);
            }
        }


        private async Task<CreationResultStatus> ExecuteAsync()
        {
            // this is checking the initial parse, which is template agnostic.
            if (_commandInput.HasParseError)
            {
                return HelpForTemplateResolution.HandleParseError(_commandInput, _telemetryLogger);
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
            {   // Only show this if there was no alias expansion.
                // ExpandedExtraArgsFiles must be checked before alias expansion - it'll get reset if there's an alias.
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.ExtraArgsCommandAfterExpansion, string.Join(" ", _commandInput.Tokens)));
            }

            if (string.IsNullOrEmpty(_commandInput.Alias))
            {
                // The --alias param is for creating / updating / deleting aliases.
                // If it's not present, try expanding aliases now.
                CreationResultStatus aliasExpansionResult = AliasSupport.CoordinateAliasExpansion(_commandInput, _aliasRegistry, _telemetryLogger);

                if (aliasExpansionResult != CreationResultStatus.Success)
                {
                    return aliasExpansionResult;
                }
            }

            if (!ConfigureLocale())
            {
                return CreationResultStatus.InvalidParamValues;
            }

            if (!Initialize())
            {
                return CreationResultStatus.Success;
            }

            bool forceCacheRebuild = _commandInput.HasDebuggingFlag("--debug:rebuildcache");
            try
            {
                await _settingsLoader.RebuildCacheFromSettingsIfNotCurrent(forceCacheRebuild).ConfigureAwait(false);
            }
            catch (EngineInitializationException eiex)
            {
                Reporter.Error.WriteLine(eiex.Message.Bold().Red());
                Reporter.Error.WriteLine(LocalizableStrings.SettingsReadError);
                return CreationResultStatus.CreateFailed;
            }

            try
            {
                if (!string.IsNullOrEmpty(_commandInput.Alias) && !_commandInput.IsHelpFlagSpecified)
                {
                    return AliasSupport.ManipulateAliasIfValid(_aliasRegistry, _commandInput.Alias, _commandInput.Tokens.ToList(), await GetAllTemplateShortNamesAsync());
                }

                if (_commandInput.CheckForUpdates || _commandInput.ApplyUpdates)
                {
                    return await EnterUpdateFlowAsync().ConfigureAwait(false);
                }

                if (_commandInput.SearchOnline)
                {
                    return await CliTemplateSearchCoordinator.SearchForTemplateMatchesAsync(EnvironmentSettings, _commandInput, _defaultLanguage).ConfigureAwait(false);
                }

                if (string.IsNullOrWhiteSpace(TemplateName))
                {
                    return await EnterMaintenanceFlow().ConfigureAwait(false);
                }

                return await EnterTemplateManipulationFlowAsync().ConfigureAwait(false);
            }
            catch (TemplateAuthoringException tae)
            {
                Reporter.Error.WriteLine(tae.Message.Bold().Red());
                return CreationResultStatus.CreateFailed;
            }
        }

        private async Task<CreationResultStatus> EnterUpdateFlowAsync()
        {
            bool applyUpdates = _commandInput.ApplyUpdates;
            CreationResultStatus success = CreationResultStatus.Success;
            var managedSourcedGroupedByProvider = await EnvironmentSettings.SettingsLoader.TemplatesSourcesManager.GetManagedSourcesGroupedByProvider().ConfigureAwait(false);
            foreach (var (provider, sources) in managedSourcedGroupedByProvider)
            {
                IReadOnlyList<CheckUpdateResult> checkUpdateResults = await provider.GetLatestVersionsAsync(sources, CancellationToken.None).ConfigureAwait(false);
                IEnumerable<CheckUpdateResult> updatesToApply = checkUpdateResults.Where(update => !update.IsLatestVersion && !string.IsNullOrWhiteSpace(update.Version));
                if (!updatesToApply.Any())
                {
                    Reporter.Output.WriteLine("All template packages are up-to-date.");
                    return CreationResultStatus.Success;
                }

                if (applyUpdates)
                {
                    Reporter.Output.WriteLine("The following template packages will be updated:");
                    foreach (CheckUpdateResult update in updatesToApply)
                    {
                        Reporter.Output.WriteLine($"  {update.Source.Identifier}, version: {update.Version}");
                    }
                    Reporter.Output.WriteLine();

                    IReadOnlyList<UpdateResult> updateResults = await provider.UpdateAsync(updatesToApply.Select(update => UpdateRequest.FromCheckUpdateResult(update)), CancellationToken.None).ConfigureAwait(false);
                    foreach (var updateResult in updateResults)
                    {
                        if (!updateResult.Success)
                        {
                            success = CreationResultStatus.CreateFailed;
                        }
                        await DisplayInstallResultAsync(updateResult.UpdateRequest.Source?.DisplayName, updateResult).ConfigureAwait(false);
                    }
                }
                else
                {
                    foreach (var updateResult in updatesToApply)
                    {
                        Reporter.Output.WriteLine(string.Format(LocalizableStrings.UpdateAvailable, updateResult.Source.DisplayName));
                        string installString = $"{updateResult.Source.Identifier}::{updateResult.Version}"; // the package::version that will be installed
                        Reporter.Output.WriteLine(string.Format(LocalizableStrings.UpdateCheck_InstallCommand, CommandName, installString));
                    }
                }
            }
            return success;
        }
        private bool ConfigureLocale()
        {
            if (!string.IsNullOrEmpty(_commandInput.Locale))
            {
                string newLocale = _commandInput.Locale;
                if (!ValidateLocaleFormat(newLocale))
                {
                    Reporter.Error.WriteLine(string.Format(LocalizableStrings.BadLocaleError, newLocale).Bold().Red());
                    return false;
                }

                EnvironmentSettings.Host.UpdateLocale(newLocale);
            }

            return true;
        }

        private bool Initialize()
        {
            bool ephemeralHiveFlag = _commandInput.HasDebuggingFlag("--debug:ephemeral-hive");

            if (ephemeralHiveFlag)
            {
                EnvironmentSettings.Host.VirtualizeDirectory(_paths.User.BaseDir);
            }

            bool reinitFlag = _commandInput.HasDebuggingFlag("--debug:reinit");
            if (reinitFlag)
            {
                _paths.Delete(_paths.User.BaseDir);
            }

            if (!_paths.Exists(_paths.User.BaseDir) || !_paths.Exists(_paths.User.FirstRunCookie))
            {
                if (!_commandInput.IsQuietFlagSpecified)
                {
                    Reporter.Output.WriteLine(LocalizableStrings.GettingReady);
                }

                ConfigureEnvironment();
                _paths.WriteAllText(_paths.User.FirstRunCookie, "");
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
            IReadOnlyCollection<ITemplateMatchInfo> allTemplates = TemplateResolver.PerformAllTemplatesQuery(await _settingsLoader.GetTemplatesAsync(default), _hostDataLoader);

            HashSet<string> allShortNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (ITemplateMatchInfo templateMatchInfo in allTemplates)
            {
                if (templateMatchInfo.Info is IShortNameList templateWithShortNameList)
                {
                    allShortNames.UnionWith(templateWithShortNameList.ShortNameList);
                }
                else
                {
                    allShortNames.Add(templateMatchInfo.Info.ShortName);
                }
            }

            return allShortNames;
        }

        private void ShowConfig()
        {
            Reporter.Output.WriteLine(LocalizableStrings.CurrentConfiguration);
            Reporter.Output.WriteLine(" ");

            TableFormatter.Print(EnvironmentSettings.SettingsLoader.Components.OfType<IMountPointFactory>(), LocalizableStrings.NoItems, "   ", '-', new Dictionary<string, Func<IMountPointFactory, object>>
            {
                {LocalizableStrings.MountPointFactories, x => x.Id},
                {LocalizableStrings.Type, x => x.GetType().FullName},
                {LocalizableStrings.Assembly, x => x.GetType().GetTypeInfo().Assembly.FullName}
            });

            TableFormatter.Print(EnvironmentSettings.SettingsLoader.Components.OfType<IGenerator>(), LocalizableStrings.NoItems, "   ", '-', new Dictionary<string, Func<IGenerator, object>>
            {
                {LocalizableStrings.Generators, x => x.Id},
                {LocalizableStrings.Type, x => x.GetType().FullName},
                {LocalizableStrings.Assembly, x => x.GetType().GetTypeInfo().Assembly.FullName}
            });
        }

        private static void ShowVersion()
        {
            Reporter.Output.WriteLine(LocalizableStrings.CommandDescription);
            Reporter.Output.WriteLine();
            int targetLength = Math.Max(LocalizableStrings.Version.Length, LocalizableStrings.CommitHash.Length);
            Reporter.Output.WriteLine($" {LocalizableStrings.Version.PadRight(targetLength)} {GitInfo.PackageVersion}");
            Reporter.Output.WriteLine($" {LocalizableStrings.CommitHash.PadRight(targetLength)} {GitInfo.CommitHash}");
        }

        private static bool ValidateLocaleFormat(string localeToCheck)
        {
            return LocaleFormatRegex.IsMatch(localeToCheck);
        }
    }
}
