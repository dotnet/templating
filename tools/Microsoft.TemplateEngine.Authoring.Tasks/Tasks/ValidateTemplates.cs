// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Authoring.Tasks.Utilities;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Edge.Settings;

namespace Microsoft.TemplateEngine.Authoring.Tasks.Tasks
{
    /// <summary>
    /// A task that exposes template localization functionality of
    /// Microsoft.TemplateEngine.TemplateLocalizer through MSBuild.
    /// </summary>
    public sealed class ValidateTemplates : Build.Utilities.Task, ICancelableTask
    {
        private volatile CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// Gets or sets the path to the template(s) to be validated.
        /// </summary>
        [Required]
        public string? TemplateLocation { get; set; }

        public override bool Execute()
        {
            if (string.IsNullOrWhiteSpace(TemplateLocation))
            {
                Log.LogError("The property 'TemplateLocation' should be set for 'ValidateTemplates' target.");
                return false;
            }

            string templateLocation = Path.GetFullPath(TemplateLocation);

            using var loggerProvider = new MSBuildLoggerProvider(Log);
            ILoggerFactory msbuildLoggerFactory = new LoggerFactory(new[] { loggerProvider });

            using CancellationTokenSource cancellationTokenSource = GetOrCreateCancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            try
            {
                using IEngineEnvironmentSettings settings = SetupSettings(msbuildLoggerFactory);
                Scanner scanner = new(settings);
                ScanResult scanResult = Task.Run(async () => await scanner.ScanAsync(
                    templateLocation!,
                    logValidationResults: false,
                    returnInvalidTemplates: true,
                    cancellationToken).ConfigureAwait(false)).GetAwaiter().GetResult();

                cancellationToken.ThrowIfCancellationRequested();

                LogResults(scanResult);
                return !Log.HasLoggedErrors && !cancellationToken.IsCancellationRequested;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
        }

        public void Cancel() => GetOrCreateCancellationTokenSource().Cancel();

        private IEngineEnvironmentSettings SetupSettings(ILoggerFactory loggerFactory)
        {
            var builtIns = new List<(Type InterfaceType, IIdentifiedComponent Instance)>();
            builtIns.AddRange(Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Components.AllComponents);
            builtIns.AddRange(Microsoft.TemplateEngine.Edge.Components.AllComponents);

            ITemplateEngineHost host = new DefaultTemplateEngineHost("template-validator", "1.0", builtIns: builtIns, loggerFactory: loggerFactory);
            IEngineEnvironmentSettings engineEnvironmentSettings = new EngineEnvironmentSettings(host, virtualizeSettings: true);

            return engineEnvironmentSettings;
        }

        private void LogResults(ScanResult scanResult)
        {
            Log.LogMessage("Location '{0}': found {1} templates.", scanResult.MountPoint.MountPointUri, scanResult.Templates.Count);
            foreach (IScanTemplateInfo template in scanResult.Templates)
            {
                string templateDisplayName = GetTemplateDisplayName(template);
                StringBuilder sb = new();

                LogValidationEntries("Template configuration", template.ValidationErrors);
                foreach (KeyValuePair<string, ILocalizationLocator> locator in template.Localizations)
                {
                    ILocalizationLocator localizationInfo = locator.Value;
                    LogValidationEntries("Localization", localizationInfo.ValidationErrors);
                }
            }

            static string GetTemplateDisplayName(IScanTemplateInfo template)
            {
                string templateName = string.IsNullOrEmpty(template.Name) ? "<no name>" : template.Name;
                return $"'{templateName}' ({template.Identity})";
            }

            void LogValidationEntries(string subCategory, IReadOnlyList<IValidationEntry> errors)
            {
                foreach (IValidationEntry error in errors.OrderByDescending(e => e.Severity))
                {
                    switch (error.Severity)
                    {
                        case IValidationEntry.SeverityLevel.Error:
                            Log.LogError(
                                subcategory: subCategory,
                                errorCode: error.Code,
                                helpKeyword: string.Empty,
                                file: error.Location?.Filename ?? string.Empty,
                                lineNumber: error.Location?.LineNumber ?? 0,
                                columnNumber: error.Location?.Position ?? 0,
                                endLineNumber: 0,
                                endColumnNumber: 0,
                                message: error.ErrorMessage);
                            break;
                        case IValidationEntry.SeverityLevel.Warning:
                            Log.LogWarning(
                                subcategory: subCategory,
                                warningCode: error.Code,
                                helpKeyword: string.Empty,
                                file: error.Location?.Filename ?? string.Empty,
                                lineNumber: error.Location?.LineNumber ?? 0,
                                columnNumber: error.Location?.Position ?? 0,
                                endLineNumber: 0,
                                endColumnNumber: 0,
                                message: error.ErrorMessage);
                            break;
                        case IValidationEntry.SeverityLevel.Info:
                            Log.LogMessage(
                                subcategory: subCategory,
                                code: error.Code,
                                helpKeyword: string.Empty,
                                file: error.Location?.Filename ?? string.Empty,
                                lineNumber: error.Location?.LineNumber ?? 0,
                                columnNumber: error.Location?.Position ?? 0,
                                endLineNumber: 0,
                                endColumnNumber: 0,
                                MessageImportance.High,
                                message: error.ErrorMessage);
                            break;
                    }
                }
            }

        }

        private CancellationTokenSource GetOrCreateCancellationTokenSource()
        {
            if (_cancellationTokenSource != null)
            {
                return _cancellationTokenSource;
            }

            CancellationTokenSource cts = new();
            if (Interlocked.CompareExchange(ref _cancellationTokenSource, cts, null) != null)
            {
                // Reference was already set. This instance is not needed.
                cts.Dispose();
            }

            return _cancellationTokenSource;
        }
    }
}
