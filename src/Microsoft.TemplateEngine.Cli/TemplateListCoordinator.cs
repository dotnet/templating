﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.Commands;
using Microsoft.TemplateEngine.Cli.Extensions;
using Microsoft.TemplateEngine.Cli.TabularOutput;
using Microsoft.TemplateEngine.Cli.TemplateResolution;
using Microsoft.TemplateEngine.Edge.Settings;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli
{
    internal class TemplateListCoordinator
    {
        private readonly IEngineEnvironmentSettings _engineEnvironmentSettings;
        private readonly TemplatePackageManager _templatePackageManager;
        private readonly TemplateCreator _templateCreator;
        private readonly IHostSpecificDataLoader _hostSpecificDataLoader;
        private readonly ITelemetryLogger _telemetryLogger;
        private readonly string? _defaultLanguage;

        internal TemplateListCoordinator(
            IEngineEnvironmentSettings engineEnvironmentSettings,
            TemplatePackageManager templatePackageManager,
            TemplateCreator templateCreator,
            IHostSpecificDataLoader hostSpecificDataLoader,
            ITelemetryLogger telemetryLogger)

        {
            _engineEnvironmentSettings = engineEnvironmentSettings ?? throw new ArgumentNullException(nameof(engineEnvironmentSettings));
            _templatePackageManager = templatePackageManager ?? throw new ArgumentNullException(nameof(templatePackageManager));
            _templateCreator = templateCreator ?? throw new ArgumentNullException(nameof(templateCreator));
            _hostSpecificDataLoader = hostSpecificDataLoader ?? throw new ArgumentNullException(nameof(hostSpecificDataLoader));
            _telemetryLogger = telemetryLogger ?? throw new ArgumentNullException(nameof(telemetryLogger));
            _defaultLanguage = engineEnvironmentSettings.GetDefaultLanguage();
        }

        /// <summary>
        /// Handles template list display (dotnet new3 --list).
        /// </summary>
        /// <param name="args">user command input.</param>
        /// <param name="cancellationToken">cancellation token.</param>
        /// <returns></returns>
        internal async Task<NewCommandStatus> DisplayTemplateGroupListAsync(
            ListCommandArgs args,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ListTemplateResolver resolver = new ListTemplateResolver(_templatePackageManager, _hostSpecificDataLoader);
            TemplateResolutionResult resolutionResult = await resolver.ResolveTemplatesAsync(args, _defaultLanguage, cancellationToken).ConfigureAwait(false);

            //IReadOnlyDictionary<string, string?>? appliedParameterMatches = resolutionResult.GetAllMatchedParametersList();
            if (resolutionResult.TemplateGroupsWithMatchingTemplateInfoAndParameters.Any())
            {
                Reporter.Output.WriteLine(
                    string.Format(
                        LocalizableStrings.TemplatesFoundMatchingInputParameters,
                       GetInputParametersString(args/*, appliedParameterMatches*/)));
                Reporter.Output.WriteLine();

                TabularOutputSettings settings = new TabularOutputSettings(_engineEnvironmentSettings.Environment, args);

                TemplateGroupDisplay.DisplayTemplateList(
                    _engineEnvironmentSettings,
                    resolutionResult.TemplateGroupsWithMatchingTemplateInfoAndParameters,
                    settings,
                    selectedLanguage: args.Language);
                return NewCommandStatus.Success;
            }
            else
            {
                // No templates found matching the following input parameter(s): {0}.
                Reporter.Error.WriteLine(
                    string.Format(
                        LocalizableStrings.NoTemplatesMatchingInputParameters,
                        GetInputParametersString(args/*, appliedParameterMatches*/))
                    .Bold().Red());

                if (resolutionResult.HasTemplateGroupMatches)
                {
                    // {0} template(s) partially matched, but failed on {1}.
                    Reporter.Error.WriteLine(
                        string.Format(
                            LocalizableStrings.TemplatesNotValidGivenTheSpecifiedFilter,
                            resolutionResult.TemplateGroups.Count(),
                            GetPartialMatchReason(resolutionResult, args/*, appliedParameterMatches*/))
                        .Bold().Red());
                }

                Reporter.Error.WriteLine();
                // To search for the templates on NuGet.org, run:
                Reporter.Error.WriteLine(LocalizableStrings.SearchTemplatesCommand);
                if (string.IsNullOrWhiteSpace(args.ListNameCriteria))
                {
                    Reporter.Error.WriteCommand(CommandExamples.SearchCommandExample(args.CommandName, usePlaceholder: true));
                }
                else
                {
                    Reporter.Error.WriteCommand(CommandExamples.SearchCommandExample(args.CommandName, args.ListNameCriteria));
                }
                Reporter.Error.WriteLine();
                return NewCommandStatus.NotFound;
            }
        }

        private static string GetInputParametersString(ListCommandArgs args/*, IReadOnlyDictionary<string, string?>? templateParameters = null*/)
        {
            string separator = ", ";
            IEnumerable<string> appliedFilters = args.AppliedFilters
                    .Select(filter => $"{args.GetFilterToken(filter)}='{args.GetFilterValue(filter)}'");

            //IEnumerable<string> appliedTemplateParameters = templateParameters?
            //       .Select(param => string.IsNullOrWhiteSpace(param.Value) ? param.Key : $"{param.Key}='{param.Value}'") ?? Array.Empty<string>();

            StringBuilder inputParameters = new StringBuilder();
            string? mainCriteria = args.ListNameCriteria;
            if (!string.IsNullOrWhiteSpace(mainCriteria))
            {
                inputParameters.Append($"'{mainCriteria}'");
                if (appliedFilters.Any()/* || appliedTemplateParameters.Any()*/)
                {
                    inputParameters.Append(separator);
                }
            }
            if (appliedFilters/*.Concat(appliedTemplateParameters)*/.Any())
            {
                inputParameters.Append(string.Join(separator, appliedFilters/*.Concat(appliedTemplateParameters)*/));
            }
            return inputParameters.ToString();
        }

        private static string GetPartialMatchReason(TemplateResolutionResult templateResolutionResult, ListCommandArgs args/*, IReadOnlyDictionary<string, string?>? templateParameters = null*/)
        {
            string separator = ", ";

            IEnumerable<string> appliedFilters = args.AppliedFilters
                    .OfType<TemplateFilterOptionDefinition>()
                    .Where(filter => filter.MismatchCriteria(templateResolutionResult))
                    .Select(filter => $"{args.GetFilterToken(filter)}='{args.GetFilterValue(filter)}'");

            //IEnumerable<string> appliedTemplateParameters = templateParameters?
            //       .Where(parameter =>
            //            templateResolutionResult.IsParameterMismatchReason(parameter.Key))
            //       .Select(param => string.IsNullOrWhiteSpace(param.Value) ? param.Key : $"{param.Key}='{param.Value}'") ?? Array.Empty<string>();

            StringBuilder inputParameters = new StringBuilder();
            if (appliedFilters/*.Concat(appliedTemplateParameters)*/.Any())
            {
                inputParameters.Append(string.Join(separator, appliedFilters/*.Concat(appliedTemplateParameters)*/));
            }
            return inputParameters.ToString();
        }
    }
}
