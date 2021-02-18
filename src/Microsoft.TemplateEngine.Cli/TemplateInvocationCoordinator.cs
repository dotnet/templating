using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Cli.NuGet;
using Microsoft.TemplateEngine.Cli.TemplateResolution;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;
using Microsoft.TemplateEngine.Utils;
using NuGet.Credentials;

namespace Microsoft.TemplateEngine.Cli
{
    internal class TemplateInvocationCoordinator
    {
        private readonly ISettingsLoader _settingsLoader;
        private readonly IEngineEnvironmentSettings _environment;
        private readonly INewCommandInput _commandInput;
        private readonly ITelemetryLogger _telemetryLogger;
        private readonly string _commandName;
        private readonly Func<string> _inputGetter;
        private readonly New3Callbacks _callbacks;

        public TemplateInvocationCoordinator(ISettingsLoader settingsLoader, INewCommandInput commandInput, ITelemetryLogger telemetryLogger,  string commandName, Func<string> inputGetter, New3Callbacks callbacks)
        {
            _settingsLoader = settingsLoader;
            _environment = _settingsLoader.EnvironmentSettings;
            _commandInput = commandInput;
            _telemetryLogger = telemetryLogger;
            _commandName = commandName;
            _inputGetter = inputGetter;
            _callbacks = callbacks;
        }

        public async Task<CreationResultStatus> CoordinateInvocationOrAcquisitionAsync(ITemplateMatchInfo templateToInvoke, CancellationToken cancellationToken)
        {
            // invoke and then check for updates
            CreationResultStatus creationResult = await InvokeTemplateAsync(templateToInvoke).ConfigureAwait(false);
            // check for updates on this template (pack)
            await CheckForTemplateUpdateAsync(templateToInvoke, cancellationToken).ConfigureAwait(false);
            return creationResult;
        }

        private Task<CreationResultStatus> InvokeTemplateAsync(ITemplateMatchInfo templateToInvoke)
        {
            TemplateInvoker invoker = new TemplateInvoker(_environment, _commandInput, _telemetryLogger, _commandName, _inputGetter, _callbacks);
            return invoker.InvokeTemplate(templateToInvoke);
        }


        private async Task CheckForTemplateUpdateAsync(ITemplateMatchInfo templateToInvoke, CancellationToken cancellationToken)
        {
            ITemplatesSource templateSource;
            try
            {
                templateSource = await templateToInvoke.Info.GetTemplateSourceAsync(_environment).ConfigureAwait(false);
            }
            catch (Exception)
            {
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.TemplatesPackage_NotFound, templateToInvoke.Info.Identity));
                return;
            }

            IManagedTemplatesSource managedTemplateSource = templateSource as IManagedTemplatesSource;
            if (managedTemplateSource is null)
            {
                //update is not supported - built-in or optional workload source
                return;
            }

            DefaultCredentialServiceUtility.SetupDefaultCredentialService(new CliNuGetLogger(), nonInteractive: !_commandInput.IsInteractiveFlagSpecified);
            IReadOnlyList<CheckUpdateResult> versionChecks = await managedTemplateSource.Installer.Provider.GetLatestVersionsAsync(new[] { managedTemplateSource }, cancellationToken).ConfigureAwait(false);
            if (versionChecks.Count == 1 && versionChecks[0].Success)
            {
                var updateResult = versionChecks[0];
                if (!updateResult.IsLatestVersion)
                {
                    string displayString = $"{updateResult.Source.Identifier}::{updateResult.Source.Version}";         // the package::version currently installed
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.UpdateAvailable, displayString));
                    string installString = $"{updateResult.Source.Identifier}::{updateResult.LatestVersion}"; // the package::version that will be installed
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.UpdateCheck_InstallCommand, _commandName, installString));
                }
            }
            else
            {
                switch (versionChecks[0]?.Error)
                {
                    case InstallerErrorCode.InvalidSource:
                        Reporter.Error.WriteLine($"Failed to check update for {managedTemplateSource.DisplayName}: no NuGet feeds are configured or they are invalid.".Bold().Red());
                        break;
                    case InstallerErrorCode.PackageNotFound:
                        Reporter.Error.WriteLine($"Failed to check update for {managedTemplateSource.DisplayName}: the package is not available in configured NuGet feed.".Bold().Red());
                        break;
                    case InstallerErrorCode.UnsupportedRequest:
                        Reporter.Error.WriteLine($"Failed to check update for {managedTemplateSource.DisplayName}: the package is not supported.".Bold().Red());
                        break;
                    case InstallerErrorCode.GenericError:
                    default:
                        Reporter.Error.WriteLine($"Failed to check update for {managedTemplateSource.DisplayName}: {versionChecks[0]?.ErrorMessage}.".Bold().Red());
                        break;
                }
            }
        }
    }
}
