// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;

namespace Microsoft.TemplateEngine.Authoring.CLI.Commands
{
    internal class ValidateCommand : ExecutableCommand<ValidateCommandArgs>
    {
        private const string CommandName = "validate";

        private readonly Argument<string> _templateLocationArg = new("template-location")
        {
            Description = LocalizableStrings.command_validate_help_description,
            Arity = new ArgumentArity(1, 1)
        };

        public ValidateCommand() : base(CommandName, "Validates the templates at given location.")
        {
            AddArgument(_templateLocationArg);
        }

        protected internal override ValidateCommandArgs ParseContext(ParseResult parseResult)
        {
            return new ValidateCommandArgs(parseResult.GetValue(_templateLocationArg));
        }

        protected override async Task<int> ExecuteAsync(ValidateCommandArgs args, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
        {
            ILogger logger = loggerFactory.CreateLogger(CommandName);
            cancellationToken.ThrowIfCancellationRequested();

            using IEngineEnvironmentSettings settings = SetupSettings(loggerFactory);
            Scanner scanner = new(settings);

            logger.LogInformation("Scanning location '{0}' for the templates...", args.TemplateLocation);

            ScanResult scanResult = await scanner.ScanAsync(
                args.TemplateLocation,
                logValidationResults: false,
                returnInvalidTemplates: true,
                cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            logger.LogInformation("Scanning completed");
            PrintResults(logger, scanResult);
            return scanResult.Templates.Any(t => !t.IsValid) ? 1 : 0;
        }

        private IEngineEnvironmentSettings SetupSettings(ILoggerFactory loggerFactory)
        {
            var builtIns = new List<(Type InterfaceType, IIdentifiedComponent Instance)>();
            builtIns.AddRange(Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Components.AllComponents);
            builtIns.AddRange(Microsoft.TemplateEngine.Edge.Components.AllComponents);

            ITemplateEngineHost host = new DefaultTemplateEngineHost("template-validator", "1.0", builtIns: builtIns, loggerFactory: loggerFactory);
            IEngineEnvironmentSettings engineEnvironmentSettings = new EngineEnvironmentSettings(host, virtualizeSettings: true);

            return engineEnvironmentSettings;
        }

        private void PrintResults(ILogger logger, ScanResult scanResult)
        {
            using var scope = logger.BeginScope("Results");
            logger.LogInformation("Location '{0}': found {1} templates.", scanResult.MountPoint.MountPointUri, scanResult.Templates.Count);
            foreach (IScanTemplateInfo template in scanResult.Templates)
            {
                string templateDisplayName = GetTemplateDisplayName(template);
                StringBuilder sb = new();

                LogValidationEntries(
                    sb,
                    string.Format("Found template {0}:", templateDisplayName),
                    template.ValidationErrors);
                foreach (KeyValuePair<string, ILocalizationLocator> locator in template.Localizations)
                {
                    ILocalizationLocator localizationInfo = locator.Value;

                    LogValidationEntries(
                        sb,
                        string.Format("Found localization {0} for template {1}:", localizationInfo.Locale, templateDisplayName),
                        localizationInfo.ValidationErrors);
                }

                if (!template.IsValid)
                {
                    sb.AppendFormat("Template {0}: the template is not valid.", templateDisplayName);
                }
                else
                {
                    sb.AppendFormat("Template {0}: the template is valid.", templateDisplayName);
                }
                sb.AppendLine();
                foreach (ILocalizationLocator loc in template.Localizations.Values)
                {
                    if (loc.IsValid)
                    {
                        sb.AppendFormat("'{0}' localization for the template {1}: the localization file is valid.", loc.Locale, templateDisplayName);
                    }
                    else
                    {
                        sb.AppendFormat("'{0}' localization for the template {1}: the localization file is not valid. The localization will be skipped.", loc.Locale, templateDisplayName);
                    }
                    sb.AppendLine();
                }
                logger.LogInformation(sb.ToString());
            }

            static string GetTemplateDisplayName(IScanTemplateInfo template)
            {
                string templateName = string.IsNullOrEmpty(template.Name) ? "<no name>" : template.Name;
                return $"'{templateName}' ({template.Identity})";
            }

            static string PrintError(IValidationEntry error) => $"   [{error.Severity}][{error.Code}] {error.ErrorMessage}";

            static void LogValidationEntries(StringBuilder sb, string header, IReadOnlyList<IValidationEntry> errors)
            {
                sb.AppendLine(header);
                if (!errors.Any())
                {
                    sb.AppendLine("   <no entries>");
                }
                else
                {
                    foreach (IValidationEntry error in errors.OrderByDescending(e => e.Severity))
                    {
                        sb.AppendLine(PrintError(error));
                    }
                }
            }

        }
    }
}
