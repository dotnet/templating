// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Cli.HelpAndUsage;
using Microsoft.TemplateEngine.Cli.NuGet;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Utils;
using NuGet.Credentials;

namespace Microsoft.TemplateEngine.Cli
{
    /// <summary>
    /// The class is responsible for template package manipulation flows: install template packages (-i, --install), check for update (--update-check), apply updates (--update-apply), uninstall template packages (-u, --uninstall).
    /// </summary>
    internal class TemplatePackageCoordinator
    {
        private ITelemetryLogger _telemetryLogger;
        private IEngineEnvironmentSettings _engineEnvironmentSettings;
        private string _defaultLanguage;

        internal TemplatePackageCoordinator(
            ITelemetryLogger telemetryLogger,
            IEngineEnvironmentSettings environmentSettings,
            string defaultLanguage = "")
        {
            _telemetryLogger = telemetryLogger;
            _engineEnvironmentSettings = environmentSettings;
            _defaultLanguage = defaultLanguage;
        }

        /// <summary>
        /// Processes template packages according to <paramref name="commandInput"/>.
        /// </summary>
        /// <param name="commandInput">the command input with instructions to process</param>
        /// <returns></returns>
        internal Task<CreationResultStatus> Process(INewCommandInput commandInput)
        {
            if (commandInput.ToUninstallList != null)
            {
                return EnterUninstallFlowAsync(commandInput);
            }

            if (commandInput.CheckForUpdates || commandInput.ApplyUpdates)
            {
                InitializeNuGetCredentialService(commandInput);
                return EnterUpdateFlowAsync(commandInput);
            }
            if (commandInput.ToInstallList != null && commandInput.ToInstallList.Count > 0 && commandInput.ToInstallList[0] != null)
            {
                InitializeNuGetCredentialService(commandInput);
                return EnterInstallFlowAsync(commandInput);
            }
            throw new NotSupportedException($"The operation is not supported, command: {commandInput}.");
        }

        /// <summary>
        /// Checks if <paramref name="commandInput"/> has instructions for template packages
        /// </summary>
        /// <param name="commandInput">the command input to check</param>
        /// <returns></returns>
        internal static bool IsTemplatePackageManipulationFlow(INewCommandInput commandInput)
        {
            if (commandInput.CheckForUpdates || commandInput.ApplyUpdates)
            {
                return true;
            }
            if (commandInput.ToUninstallList != null)
            {
                return true;
            }
            if (commandInput.ToInstallList != null && commandInput.ToInstallList.Count > 0 && commandInput.ToInstallList[0] != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if there is an update for the package contiaing the <paramref name="template"/>.
        /// </summary>
        /// <param name="template">template to check the update for</param>
        /// <param name="commandInput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        internal async Task CheckUpdateForTemplate(ITemplateInfo template, INewCommandInput commandInput, CancellationToken cancellationToken)
        {
            ITemplatesSource templateSource;
            try
            {
                templateSource = await template.GetTemplateSourceAsync(_engineEnvironmentSettings).ConfigureAwait(false);
            }
            catch (Exception)
            {
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.TemplatesPackage_NotFound, template.Identity));
                return;
            }

            IManagedTemplatesSource managedTemplateSource = templateSource as IManagedTemplatesSource;
            if (managedTemplateSource is null)
            {
                //update is not supported - built-in or optional workload source
                return;
            }

            InitializeNuGetCredentialService(commandInput);
            IReadOnlyList<CheckUpdateResult> versionChecks = await managedTemplateSource.Installer.Provider.GetLatestVersionsAsync(new[] { managedTemplateSource }, cancellationToken).ConfigureAwait(false);
            DisplayUpdateCheckResults(versionChecks, commandInput, showUpdates: true);
        }

        /// <summary>
        /// Install the template package(s) flow (--install, -i)
        /// </summary>
        private async Task<CreationResultStatus> EnterInstallFlowAsync(INewCommandInput commandInput)
        {
            CreationResultStatus resultStatus = CreationResultStatus.Success;
            _telemetryLogger.TrackEvent(commandInput.CommandName + TelemetryConstants.InstallEventSuffix, new Dictionary<string, string> { { TelemetryConstants.ToInstallCount, commandInput.ToInstallList.Count.ToString() } });

            var details = new Dictionary<string, string>();
            if (commandInput.InstallNuGetSourceList?.Count > 0)
            {
                details[InstallerConstants.NuGetSourcesKey] = string.Join(InstallerConstants.NuGetSourcesSeparator.ToString(), commandInput.InstallNuGetSourceList);
            }
            if (commandInput.IsInteractiveFlagSpecified)
            {
                details[InstallerConstants.InteractiveModeKey] = "true";
            }

            // In future we might want give user ability to pick IManagerSourceProvider by Name or GUID
            var managedSourceProvider = _engineEnvironmentSettings.SettingsLoader.TemplatesSourcesManager.GetManagedProvider(GlobalSettingsTemplatesSourcesProviderFactory.FactoryId);
            List<InstallRequest> installRequests = new List<InstallRequest>();
            foreach (string unexpandedInstallRequest in commandInput.ToInstallList)
            {
                foreach (var expandedInstallRequest in InstallRequestPathResolution.Expand(unexpandedInstallRequest, _engineEnvironmentSettings))
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
                await DisplayInstallResultAsync(commandInput, result.InstallRequest.DisplayName, result).ConfigureAwait(false);
                if (!result.Success)
                {
                    resultStatus = CreationResultStatus.CreateFailed;
                }
            }
            return resultStatus;
        }

        /// <summary>
        /// Update the template package(s) flow (--update-check and --update-apply)
        /// </summary>
        private async Task<CreationResultStatus> EnterUpdateFlowAsync(INewCommandInput commandInput)
        {
            bool applyUpdates = commandInput.ApplyUpdates;
            bool allTemplatesUpToDate = true;
            CreationResultStatus success = CreationResultStatus.Success;
            var managedSourcedGroupedByProvider = await _engineEnvironmentSettings.SettingsLoader.TemplatesSourcesManager.GetManagedSourcesGroupedByProvider().ConfigureAwait(false);

            foreach (var (provider, sources) in managedSourcedGroupedByProvider)
            {
                IReadOnlyList<CheckUpdateResult> checkUpdateResults = await provider.GetLatestVersionsAsync(sources, CancellationToken.None).ConfigureAwait(false);
                DisplayUpdateCheckResults(checkUpdateResults, commandInput, showUpdates: !applyUpdates);
                if (checkUpdateResults.Any(result => !result.Success))
                {
                    success = CreationResultStatus.CreateFailed;
                }
                allTemplatesUpToDate = checkUpdateResults.All(result => result.Success && result.IsLatestVersion);

                if (applyUpdates)
                {
                    IEnumerable<CheckUpdateResult> updatesToApply = checkUpdateResults.Where(update => update.Success && !update.IsLatestVersion && !string.IsNullOrWhiteSpace(update.LatestVersion));
                    if (!updatesToApply.Any())
                    {
                        continue;
                    }

                    Reporter.Output.WriteLine("The following template packages will be updated:");
                    foreach (CheckUpdateResult update in updatesToApply)
                    {
                        Reporter.Output.WriteLine($"  {update.Source.Identifier}, version: {update.LatestVersion}");
                    }
                    Reporter.Output.WriteLine();

                    IReadOnlyList<UpdateResult> updateResults = await provider.UpdateAsync(updatesToApply.Select(update => UpdateRequest.FromCheckUpdateResult(update)), CancellationToken.None).ConfigureAwait(false);
                    foreach (var updateResult in updateResults)
                    {
                        if (!updateResult.Success)
                        {
                            success = CreationResultStatus.CreateFailed;
                        }
                        await DisplayInstallResultAsync(commandInput, updateResult.UpdateRequest.Source?.DisplayName, updateResult).ConfigureAwait(false);
                    }
                }
            }

            if (allTemplatesUpToDate)
            {
                Reporter.Output.WriteLine("All template packages are up-to-date.");
            }

            return success;
        }

        /// <summary>
        /// Uninstall the template package(s) flow (--uninstall, -u)
        /// </summary>
        private async Task<CreationResultStatus> EnterUninstallFlowAsync(INewCommandInput commandInput)
        {
            CreationResultStatus result = CreationResultStatus.Success;
            if (commandInput.ToUninstallList.Count > 0 && commandInput.ToUninstallList[0] != null)
            {
                var managedSourcedGroupedByProvider = await _engineEnvironmentSettings.SettingsLoader.TemplatesSourcesManager.GetManagedSourcesGroupedByProvider().ConfigureAwait(false);
                bool sourceToUninstallFound = false;
                foreach (var (provider, sources) in managedSourcedGroupedByProvider)
                {
                    var sourcesToUninstall = sources.Where(source => commandInput.ToUninstallList.Contains(source.Identifier, StringComparer.OrdinalIgnoreCase));
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
                    await DisplayInstalledTemplatesSources(commandInput).ConfigureAwait(false);
                }
            }
            else
            {
                await DisplayInstalledTemplatesSources(commandInput).ConfigureAwait(false);
            }

            return result;
        }

        private void DisplayUpdateCheckResults(IEnumerable<CheckUpdateResult> versionCheckResults, INewCommandInput commandInput, bool showUpdates = true)
        {
            _ = versionCheckResults ?? throw new ArgumentNullException(nameof(versionCheckResults));

            foreach (CheckUpdateResult result in versionCheckResults)
            {
                if (result.Success)
                {
                    if (!result.IsLatestVersion && showUpdates)
                    {
                        string displayString = $"{result.Source.Identifier}::{result.Source.Version}";         // the package::version currently installed
                        Reporter.Output.WriteLine(string.Format(LocalizableStrings.UpdateAvailable, displayString));
                        string installString = $"{result.Source.Identifier}::{result.LatestVersion}"; // the package::version that will be installed
                        Reporter.Output.WriteLine(string.Format(LocalizableStrings.UpdateCheck_InstallCommand, commandInput.CommandName, installString));
                    }
                }
                else
                {
                    switch (result.Error)
                    {
                        case InstallerErrorCode.InvalidSource:
                            Reporter.Error.WriteLine($"Failed to check update for {result.Source.DisplayName}: no NuGet feeds are configured or they are invalid.".Bold().Red());
                            break;
                        case InstallerErrorCode.PackageNotFound:
                            Reporter.Error.WriteLine($"Failed to check update for {result.Source.DisplayName}: the package is not available in configured NuGet feed.".Bold().Red());
                            break;
                        case InstallerErrorCode.UnsupportedRequest:
                            Reporter.Error.WriteLine($"Failed to check update for {result.Source.DisplayName}: the package is not supported.".Bold().Red());
                            break;
                        case InstallerErrorCode.GenericError:
                        default:
                            Reporter.Error.WriteLine($"Failed to check update for {result.Source.DisplayName}: {result.ErrorMessage}.".Bold().Red());
                            break;
                    }
                }
            }
        }

        private async Task DisplayInstalledTemplatesSources(INewCommandInput commandInput)
        {
            IEnumerable<IManagedTemplatesSource> managedTemplatesSources = await _engineEnvironmentSettings.SettingsLoader.TemplatesSourcesManager.GetManagedTemplatesSources().ConfigureAwait(false);

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

                IEnumerable<ITemplateInfo> templates = await managedSource.GetTemplates(_engineEnvironmentSettings).ConfigureAwait(false);
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
                Reporter.Output.WriteLine(string.Format("      dotnet {0} -u {1}", commandInput.CommandName, managedSource.Identifier));

                Reporter.Output.WriteLine();
            }
        }

        private async Task DisplayInstallResultAsync(INewCommandInput commandInput, string packageToInstall, Result result)
        {
            if (result.Success)
            {
                Reporter.Output.WriteLine($"Success: {result.Source.DisplayName} installed the following templates:");
                IEnumerable<ITemplateInfo> templates = await result.Source.GetTemplates(_engineEnvironmentSettings).ConfigureAwait(false);
                HelpForTemplateResolution.DisplayTemplateList(templates, _engineEnvironmentSettings, commandInput, _defaultLanguage);
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
                    case InstallerErrorCode.AlreadyInstalled:
                        Reporter.Error.WriteLine($"{packageToInstall} is already installed.".Bold().Red());
                        break;
                    case InstallerErrorCode.UpdateUninstallFailed:
                        Reporter.Error.WriteLine($"Failed to install {packageToInstall}, failed to uninstall previous version of the template package.".Bold().Red());
                        break;
                    case InstallerErrorCode.InvalidPackage:
                        Reporter.Error.WriteLine($"Failed to install {packageToInstall}, the template package is invalid.".Bold().Red());
                        break;
                    case InstallerErrorCode.GenericError:
                    default:
                        Reporter.Error.WriteLine(string.Format(LocalizableStrings.InstallFailedGenericError, packageToInstall).Bold().Red());
                        break;
                }
            }
        }

        private static void InitializeNuGetCredentialService(INewCommandInput commandInput)
        {
            try
            {
                DefaultCredentialServiceUtility.SetupDefaultCredentialService(new CliNuGetLogger(), !commandInput.IsInteractiveFlagSpecified);
            }
            catch (Exception ex)
            {
                Reporter.Verbose.WriteLine($"Failed to initialize NuGet credential service, details: {ex.ToString()}.");
            }
        }
    }
}
