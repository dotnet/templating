using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli
{
    internal class TemplateInvocationCoordinator
    {
        private readonly SettingsLoader _settingsLoader;
        private readonly IEngineEnvironmentSettings _environment;
        private readonly INewCommandInput _commandInput;
        private readonly ITelemetryLogger _telemetryLogger;
        private readonly string _commandName;
        private readonly Func<string> _inputGetter;
        private readonly New3Callbacks _callbacks;

        public TemplateInvocationCoordinator(SettingsLoader settingsLoader, INewCommandInput commandInput, ITelemetryLogger telemetryLogger,  string commandName, Func<string> inputGetter, New3Callbacks callbacks)
        {
            _settingsLoader = settingsLoader;
            _environment = _settingsLoader.EnvironmentSettings;
            _commandInput = commandInput;
            _telemetryLogger = telemetryLogger;
            _commandName = commandName;
            _inputGetter = inputGetter;
            _callbacks = callbacks;
        }

        public async Task<CreationResultStatus> CoordinateInvocationOrAcquisitionAsync(ITemplateMatchInfo templateToInvoke)
        {
            // invoke and then check for updates
            CreationResultStatus creationResult = await InvokeTemplateAsync(templateToInvoke).ConfigureAwait(false);
            // check for updates on this template (pack)
            await CheckForTemplateUpdateAsync(templateToInvoke).ConfigureAwait(false);
            return creationResult;
        }

        private Task<CreationResultStatus> InvokeTemplateAsync(ITemplateMatchInfo templateToInvoke)
        {
            TemplateInvoker invoker = new TemplateInvoker(_environment, _commandInput, _telemetryLogger, _commandName, _inputGetter, _callbacks);
            return invoker.InvokeTemplate(templateToInvoke);
        }

        // check for updates for the matched template, based on the Identity
        private async Task CheckForTemplateUpdateAsync(ITemplateMatchInfo templateToInvoke)
        {
            var getSources = await _settingsLoader.TemplatesSourcesManager.GetManagedTemplatesSources();
            var sourceOfThisTemplate = getSources.FirstOrDefault(s => s.MountPointUri == templateToInvoke.Info.MountPointUri);
            if (sourceOfThisTemplate == null)
            {
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.InstallDescriptor_NotFound, templateToInvoke.Info.Identity));
                return;
            }

            var versionChecks = await sourceOfThisTemplate.ManagedProvider.GetLatestVersions(new[] { sourceOfThisTemplate });

            if (versionChecks.Count == 1)
            {
                var updateResult = versionChecks[0];

                if (updateResult.Version != updateResult.InstallUnitDescriptor.Version)
                {
                    string displayString = $"{updateResult.InstallUnitDescriptor.Identifier}::{updateResult.InstallUnitDescriptor.Version}";         // the package::version currently installed
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.UpdateAvailable, displayString));
                    string installString = $"{updateResult.InstallUnitDescriptor.Identifier}::{updateResult.Version}"; // the package::version that will be installed
                    Reporter.Output.WriteLine(string.Format(LocalizableStrings.UpdateCheck_InstallCommand, _commandName, installString));
                }
            }
            else
            {
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.UpdateCheck_UnknownError, sourceOfThisTemplate.Identifier));
            }
        }
    }
}
