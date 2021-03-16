// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.TemplateLocalizer.Core;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Commands.Export
{
    internal sealed class ExportCommand : ModelBoundExecutableCommand<ExportCommandArgs>
    {
        private const string CommandName = "export";

        private const int ConcurrencyLevel = 16;

        public override Command CreateCommand()
        {
            var exportCommand = new Command(CommandName, LocalizableStrings.command_export_help_description);
            exportCommand.AddArgument(new Argument("template-path")
            {
                Arity = ArgumentArity.OneOrMore,
                ArgumentType = typeof(string),
                Description = LocalizableStrings.command_export_help_templatePath_description,
            });
            exportCommand.AddOption(new Option("-r")
            {
                Name = "--recursive",
                Description = LocalizableStrings.command_export_help_recursive_description,
            });
            exportCommand.AddOption(new Option("-l")
            {
                Name = "--language",
                Description = LocalizableStrings.command_export_help_language_description,
                Argument = new Argument()
                {
                    Arity = ArgumentArity.OneOrMore,
                    ArgumentType = typeof(string),
                },
            });
            exportCommand.AddOption(new Option("-d")
            {
                Name = "--DryRun",
                Description = LocalizableStrings.command_export_help_dryrun_description,
            });
            exportCommand.Handler = this;

            return exportCommand;
        }

        protected override async Task<int> Execute(ExportCommandArgs args, CancellationToken cancellationToken)
        {
            // Upgrade the cancellation token to allow also cancelling within this method.
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationToken = cts.Token;

            bool failed = false;
            List<FileInfo> templateJsonFiles = new List<FileInfo>();

            foreach (TemplateJsonProvider templateJsonProvider in args.TemplateJsonProviders)
            {
                int filesBeforeAdd = templateJsonFiles.Count;
                templateJsonFiles.AddRange(templateJsonProvider.GetTemplateJsonFiles(args.SearchSubdirectories));

                if (filesBeforeAdd == templateJsonFiles.Count)
                {
                    // No new files has been added by this provider. This is an indication of a bad input.
                    Logger.LogError(LocalizableStrings.command_export_log_templateJsonNotFound, templateJsonProvider.Path);
                    failed = true;
                }
            }

            if (failed)
            {
                Logger.LogError(LocalizableStrings.generic_log_commandExecutionFailed, CommandName);
                return 1;
            }

            ExportOptions exportOptions = new ExportOptions()
            {
                Languages = args.Languages,
                DryRun = args.DryRun,
            };

            List<ExportResult> exportResults = new List<ExportResult>();
            List<Task<ExportResult>> runningExportTasks = new List<Task<ExportResult>>(templateJsonFiles.Count);
            foreach (FileInfo templateJsonFile in templateJsonFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                string templateDirectory = Path.GetDirectoryName(templateJsonFile.FullName) ?? string.Empty;
                exportOptions.TargetDirectory = Path.Combine(templateDirectory, "localize");

                runningExportTasks.Add(new Core.TemplateLocalizer(Logger).ExportLocalizationFiles(templateJsonFile.FullName, exportOptions, cancellationToken));

                if (runningExportTasks.Count == ConcurrencyLevel)
                {
                    // We have reached the concurrency limit. Wait for one of the tasks to finish before issuing new ones.
                    Task<ExportResult>? completedTask = null;

                    try
                    {
                        completedTask = await Task.WhenAny(runningExportTasks).ConfigureAwait(false);
                        ExportResult taskResult = await completedTask.ConfigureAwait(false);
                        exportResults.Add(taskResult);
                    }
                    catch (OperationCanceledException)
                    {
                        // Task was intentionally cancelled.
                        Logger.LogInformation(LocalizableStrings.command_export_log_cancelled);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(LocalizableStrings.generic_log_commandExecutionFailedWithErrorMessage, e.Message);
                        // Fatal error. Stop processing the rest of the files.
                        cts.Cancel();
                        failed = true;
                        break;
                    }

                    if (completedTask != null)
                    {
                        runningExportTasks.Remove(completedTask);
                    }
                }
            }

            try
            {
                // Await all the remaining work.
                await Task.WhenAll(runningExportTasks).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Task.WhenAll will only throw one of the exceptions. We need to log them all. Handle this outside of catch block.
            }

            foreach (Task<ExportResult> completedTask in runningExportTasks)
            {
                if (completedTask.IsCanceled)
                {
                    continue;
                }
                else if (completedTask.IsFaulted)
                {
                    Logger.LogError(LocalizableStrings.generic_log_commandExecutionFailedWithErrorMessage, completedTask.Exception?.Flatten().InnerException?.Message);
                }
                else
                {
                    // Tasks is known to have already completed. We can get the result without await.
                    exportResults.Add(completedTask.Result);
                }
            }

            if (failed)
            {
                Logger.LogError(LocalizableStrings.generic_log_commandExecutionFailed, CommandName);
                return 1;
            }

            PrintResults(exportResults);
            return cts.IsCancellationRequested ? 1 : 0;
        }

        private void PrintResults(IReadOnlyList<ExportResult> results)
        {
            Logger.LogInformation(LocalizableStrings.command_export_log_executionEnded, results.Count);

            foreach (ExportResult result in results)
            {
                if (result.Succeeded)
                {
                    Logger.LogInformation(LocalizableStrings.command_export_log_templateExportSucceeded, result.TemplateJsonPath);
                }
                else
                {
                    if (result.InnerException != null)
                    {
                        Logger.LogError(result.InnerException, LocalizableStrings.command_export_log_templateExportFailedWithException, result.TemplateJsonPath);
                    }
                    else
                    {
                        Logger.LogError(LocalizableStrings.command_export_log_templateExportFailedWithError, result.TemplateJsonPath, result.ErrorMessage);
                    }
                }
            }
        }
    }
}
