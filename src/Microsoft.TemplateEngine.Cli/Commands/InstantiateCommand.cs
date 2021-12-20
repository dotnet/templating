﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.Extensions;
using Microsoft.TemplateEngine.Edge.Settings;

namespace Microsoft.TemplateEngine.Cli.Commands
{
    internal partial class InstantiateCommand : BaseCommand<InstantiateCommandArgs>, ICustomHelp
    {
        private NewCommand? _parentCommand;

        internal InstantiateCommand(
            ITemplateEngineHost host,
            ITelemetryLogger logger,
            NewCommandCallbacks callbacks)
            : base(host, logger, callbacks, "create", SymbolStrings.Command_Instantiate_Description)
        {
            this.AddArgument(ShortNameArgument);
            this.AddArgument(RemainingArguments);
            IsHidden = true;
        }

        private InstantiateCommand(NewCommand parentCommand, string name, string description)
            : base(parentCommand, name, description)
        {
            _parentCommand = parentCommand;
            this.AddArgument(ShortNameArgument);
            this.AddArgument(RemainingArguments);
        }

        internal Argument<string> ShortNameArgument { get; } = new Argument<string>("template-short-name")
        {
            Description = SymbolStrings.Command_Instantiate_Argument_ShortName,
            Arity = new ArgumentArity(0, 1)
        };

        internal Argument<string[]> RemainingArguments { get; } = new Argument<string[]>("template-args")
        {
            Description = SymbolStrings.Command_Instantiate_Argument_TemplateOptions,
            Arity = new ArgumentArity(0, 999)
        };

        internal static InstantiateCommand FromNewCommand(NewCommand parentCommand)
        {
            InstantiateCommand command = new InstantiateCommand(
                parentCommand,
                parentCommand.Name,
                parentCommand.Description ?? SymbolStrings.Command_New_Description);
            //subcommands are re-added just for the sake of proper help display
            foreach (var subcommand in parentCommand.Children.OfType<Command>())
            {
                command.Add(subcommand);
            }
            return command;
        }

        internal Task<NewCommandStatus> ExecuteAsync(ParseResult parseResult, IEngineEnvironmentSettings environmentSettings, InvocationContext context)
        {
            return ExecuteAsync(ParseContext(parseResult), environmentSettings, context);
        }

        internal HashSet<TemplateCommand> GetTemplateCommand(
                InstantiateCommandArgs args,
                IEngineEnvironmentSettings environmentSettings,
                TemplatePackageManager templatePackageManager,
                TemplateGroup templateGroup)
        {
            //groups templates in the group by precedence
            foreach (IGrouping<int, CliTemplateInfo> templateGrouping in templateGroup.Templates.GroupBy(g => g.Precedence).OrderByDescending(g => g.Key))
            {
                HashSet<TemplateCommand> candidates = ReparseForTemplate(
                    args,
                    environmentSettings,
                    templatePackageManager,
                    templateGroup,
                    templateGrouping,
                    out bool languageOptionSpecified);

                //if no candidates continue with next precedence
                if (!candidates.Any())
                {
                    continue;
                }
                //if language option is not specified, we do not need to do reparsing for default language
                if (languageOptionSpecified || string.IsNullOrWhiteSpace(environmentSettings.GetDefaultLanguage()))
                {
                    return candidates;
                }

                // try to reparse for default language
                return ReparseForDefaultLanguage(
                    args,
                    environmentSettings,
                    templatePackageManager,
                    templateGroup,
                    candidates);
            }
            return new HashSet<TemplateCommand>();
        }

        internal IEngineEnvironmentSettings GetEnvironmentSettingsFromArgs(InstantiateCommandArgs instantiateArgs)
        {
            return this.CreateEnvironmentSettings(instantiateArgs, instantiateArgs.ParseResult);
        }

        internal async Task<IEnumerable<TemplateGroup>> GetMatchingTemplateGroupsAsync(
            InstantiateCommandArgs instantiateArgs,
            TemplatePackageManager templatePackageManager,
            HostSpecificDataLoader hostSpecificDataLoader,
            CancellationToken cancellationToken)
        {
            var templates = await templatePackageManager.GetTemplatesAsync(cancellationToken).ConfigureAwait(false);
            var templateGroups = TemplateGroup.FromTemplateList(CliTemplateInfo.FromTemplateInfo(templates, hostSpecificDataLoader));
            return templateGroups.Where(template => template.ShortNames.Contains(instantiateArgs.ShortName));
        }

        internal void HandleNoMatchingTemplateGroup(InstantiateCommandArgs instantiateArgs, Reporter reporter)
        {
            reporter.WriteLine(
                string.Format(LocalizableStrings.NoTemplatesMatchingInputParameters, $"'{instantiateArgs.ShortName}'").Bold().Red());
            reporter.WriteLine();

            reporter.WriteLine(LocalizableStrings.ListTemplatesCommand);
            reporter.WriteCommand(CommandExamples.ListCommandExample(instantiateArgs.CommandName));

            reporter.WriteLine(LocalizableStrings.SearchTemplatesCommand);
            reporter.WriteCommand(CommandExamples.SearchCommandExample(instantiateArgs.CommandName, instantiateArgs.ShortName));
            reporter.WriteLine();
        }

        internal NewCommandStatus HandleAmbiguousTemplateGroup(InstantiateCommandArgs instantiateArgs) => throw new NotImplementedException();

        protected async override Task<NewCommandStatus> ExecuteAsync(InstantiateCommandArgs instantiateArgs, IEngineEnvironmentSettings environmentSettings, InvocationContext context)
        {
            var cancellationToken = context.GetCancellationToken();
            using TemplatePackageManager templatePackageManager = new TemplatePackageManager(environmentSettings);
            HostSpecificDataLoader hostSpecificDataLoader = new HostSpecificDataLoader(environmentSettings);
            if (string.IsNullOrWhiteSpace(instantiateArgs.ShortName))
            {
                TemplateListCoordinator templateListCoordinator = new TemplateListCoordinator(
                    environmentSettings,
                    templatePackageManager,
                    hostSpecificDataLoader,
                    TelemetryLogger);

                return await templateListCoordinator.DisplayCommandDescriptionAsync(instantiateArgs, cancellationToken).ConfigureAwait(false);
            }

            var selectedTemplateGroups = await GetMatchingTemplateGroupsAsync(
                instantiateArgs,
                templatePackageManager,
                hostSpecificDataLoader,
                cancellationToken).ConfigureAwait(false);

            if (!selectedTemplateGroups.Any())
            {
                HandleNoMatchingTemplateGroup(instantiateArgs, Reporter.Error);
                return NewCommandStatus.NotFound;
            }
            if (selectedTemplateGroups.Count() > 1)
            {
                return HandleAmbiguousTemplateGroup(instantiateArgs);
            }
            return await HandleTemplateInstantationAsync(
                instantiateArgs,
                environmentSettings,
                templatePackageManager,
                selectedTemplateGroups.Single(),
                cancellationToken).ConfigureAwait(false);
        }

        protected override InstantiateCommandArgs ParseContext(ParseResult parseResult) => new(this, parseResult);

        private async Task<NewCommandStatus> HandleTemplateInstantationAsync(
            InstantiateCommandArgs args,
            IEngineEnvironmentSettings environmentSettings,
            TemplatePackageManager templatePackageManager,
            TemplateGroup templateGroup,
            CancellationToken cancellationToken)
        {
            HashSet<TemplateCommand> candidates = GetTemplateCommand(args, environmentSettings, templatePackageManager, templateGroup);
            if (candidates.Count == 1)
            {
                Command commandToRun = _parentCommand is null ? this : _parentCommand;
                commandToRun.AddCommand(candidates.First());
                return (NewCommandStatus)await commandToRun.InvokeAsync(args.TokensToInvoke).ConfigureAwait(false);
            }
            else if (candidates.Any())
            {
                return HandleAmbuguousResult();
            }

            return HandleNoTemplateFoundResult(args, environmentSettings, templatePackageManager, templateGroup, Reporter.Error);
        }

        private NewCommandStatus HandleAmbuguousResult() => throw new NotImplementedException();

        private HashSet<TemplateCommand> ReparseForTemplate(
            InstantiateCommandArgs args,
            IEngineEnvironmentSettings environmentSettings,
            TemplatePackageManager templatePackageManager,
            TemplateGroup templateGroup,
            IEnumerable<CliTemplateInfo> templatesToReparse,
            out bool languageOptionSpecified)
        {
            languageOptionSpecified = false;
            HashSet<TemplateCommand> candidates = new HashSet<TemplateCommand>();
            foreach (CliTemplateInfo template in templatesToReparse)
            {
                TemplateCommand command = new TemplateCommand(this, environmentSettings, templatePackageManager, templateGroup, template);
                Parser parser = ParserFactory.CreateParser(command);
                ParseResult parseResult = parser.Parse(args.RemainingArguments ?? Array.Empty<string>());

                languageOptionSpecified = command.LanguageOption != null
                    && parseResult.FindResultFor(command.LanguageOption) != null;
                if (!parseResult.Errors.Any())
                {
                    candidates.Add(command);
                }
            }
            return candidates;
        }

        private HashSet<TemplateCommand> ReparseForDefaultLanguage(
            InstantiateCommandArgs args,
            IEngineEnvironmentSettings environmentSettings,
            TemplatePackageManager templatePackageManager,
            TemplateGroup templateGroup,
            HashSet<TemplateCommand> candidates)
        {
            HashSet<TemplateCommand> languageAwareCandidates = new HashSet<TemplateCommand>();
            foreach (var templateCommand in candidates)
            {
                TemplateCommand command = new TemplateCommand(
                    this,
                    environmentSettings,
                    templatePackageManager,
                    templateGroup,
                    templateCommand.Template,
                    buildDefaultLanguageValidation: true);
                Parser parser = ParserFactory.CreateParser(command);
                ParseResult parseResult = parser.Parse(args.RemainingArguments ?? Array.Empty<string>());

                if (!parseResult.Errors.Any())
                {
                    languageAwareCandidates.Add(command);
                }
            }
            return languageAwareCandidates.Any()
                ? languageAwareCandidates
                : candidates;
        }
    }

    internal class InstantiateCommandArgs : GlobalArgs
    {
        public InstantiateCommandArgs(InstantiateCommand command, ParseResult parseResult) : base(command, parseResult)
        {
            RemainingArguments = parseResult.GetValueForArgument(command.RemainingArguments) ?? Array.Empty<string>();
            ShortName = parseResult.GetValueForArgument(command.ShortNameArgument);

            var tokens = new List<string>();
            if (!string.IsNullOrWhiteSpace(ShortName))
            {
                tokens.Add(ShortName);
            }
            tokens.AddRange(RemainingArguments);
            TokensToInvoke = tokens.ToArray();

        }

        internal string? ShortName { get; }

        internal string[] RemainingArguments { get; }

        internal string[] TokensToInvoke { get; }
    }
}
