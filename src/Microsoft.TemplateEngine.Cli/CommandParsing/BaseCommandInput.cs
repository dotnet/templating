// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.TemplateEngine.Cli.Commands;

namespace Microsoft.TemplateEngine.Cli.CommandParsing
{
    internal class BaseCommandInput : INewCommandInput
    {
        internal const string AuthorColumnFilter = "author";
        internal const string LanguageColumnFilter = "language";
        internal const string TagsColumnFilter = "tags";
        internal const string TypeColumnFilter = "type";

        internal static readonly string[] SupportedFilterableColumnNames = new[]
        {
            AuthorColumnFilter,
            TypeColumnFilter,
            LanguageColumnFilter,
            TagsColumnFilter
        };

        internal BaseCommandInput(string commandName, ParseResult parseResult, string templateName)
        {
            CommandName = commandName;
            CommandParseResult = parseResult;
            TemplateName = templateName;
        }

        public string Alias => CommandParseResult.GetArgumentValueAtPath(new[] { CommandName, "alias" });

        public string? AllowScriptsToRun
        {
            get
            {
                if (CommandParseResult.TryGetArgumentValueAtPath(out string argValue, new[] { CommandName, "allow-scripts" }))
                {
                    return argValue;
                }
                return null;
            }
        }

        public bool ApplyUpdates => CommandParseResult.HasAppliedOption(new[] { CommandName, "update-apply" });

        public string AuthorFilter => CommandParseResult.GetArgumentValueAtPath(new[] { CommandName, "author" });

        public string BaselineName => CommandParseResult.GetArgumentValueAtPath(new[] { CommandName, "baseline" });

        public bool CheckForUpdates => CommandParseResult.HasAppliedOption(new[] { CommandName, "update-check" });

        public IReadOnlyList<string> Columns
        {
            get
            {
                string columnNames = CommandParseResult.GetArgumentValueAtPath(new[] { CommandName, "columns" });
                if (!string.IsNullOrWhiteSpace(columnNames))
                {
                    return columnNames.Split(',').Select(s => s.Trim()).ToList();
                }
                else
                {
                    return new List<string>();
                }
            }
        }

        public string ColumnsParseError
        {
            get
            {
                List<string> invalidColumns = new List<string>(Columns.Count);
                foreach (string columnToShow in Columns)
                {
                    if (!SupportedFilterableColumnNames.Contains(columnToShow))
                    {
                        invalidColumns.Add(columnToShow);
                    }
                }
                if (invalidColumns.Any())
                {
                    return string.Format(
                         LocalizableStrings.ColumnNamesAreNotSupported,
                         string.Join(", ", invalidColumns.Select(s => $"'{s}'")),
                         string.Join(", ", SupportedFilterableColumnNames.Select(s => $"'{s}'")));
                }
                return string.Empty;
            }
        }

        public string CommandName { get; private set; }

        public IEnumerable<string> Errors => CommandParseResult.Errors.Select(error => $"{error.Option}: {error.Message}");

        public bool ExpandedExtraArgsFiles { get; private set; }

        public IReadOnlyList<string>? ExtraArgsFileNames =>
            CommandParseResult.GetArgumentListAtPath(new[] { CommandName, "extra-args" })?.ToArray();

        public bool HasColumnsParseError => Columns.Any(column => !SupportedFilterableColumnNames.Contains(column));

        public string HelpText
        {
            get
            {
                return CommandParserSupport.CreateNewCommandForNoTemplateName(CommandName).HelpView();
            }
        }

        public IReadOnlyList<string>? InstallNuGetSourceList =>
            CommandParseResult.GetArgumentListAtPath(new[] { CommandName, "nuget-source" })?.ToArray();

        public bool IsDryRun => CommandParseResult.HasAppliedOption(new[] { CommandName, "dry-run" });

        public bool IsForceFlagSpecified => CommandParseResult.HasAppliedOption(new[] { CommandName, "force" });

        public bool IsHelpFlagSpecified => CommandParseResult.HasAppliedOption(new[] { CommandName, "help" });

        public bool IsInteractiveFlagSpecified => CommandParseResult.HasAppliedOption(new[] { CommandName, "interactive" });

        public bool IsListFlagSpecified => CommandParseResult.HasAppliedOption(new[] { CommandName, "list" });

        public bool IsQuietFlagSpecified => CommandParseResult.HasAppliedOption(new[] { CommandName, "quiet" });

        public bool IsShowAllFlagSpecified => CommandParseResult.HasAppliedOption(new[] { CommandName, "all" });

        public string Language => CommandParseResult.GetArgumentValueAtPath(new[] { CommandName, "language" });

        public bool NoUpdateCheck => CommandParseResult.HasAppliedOption(new[] { CommandName, "no-update-check" });

        public string Name => CommandParseResult.GetArgumentValueAtPath(new[] { CommandName, "name" });

        public string OutputPath => CommandParseResult.GetArgumentValueAtPath(new[] { CommandName, "output" });

        public string PackageFilter => CommandParseResult.GetArgumentValueAtPath(new[] { CommandName, "package" });

        public IReadOnlyList<string> RemainingParameters => CommandParseResult.UnmatchedTokens.ToList();

        public bool IsSearchFlagSpecified => CommandParseResult.HasAppliedOption(new[] { CommandName, "search" });

        public string ShowAliasesAliasName => CommandParseResult.GetArgumentValueAtPath(new[] { CommandName, "show-alias" });

        public bool ShowAliasesSpecified => CommandParseResult.HasAppliedOption(new[] { CommandName, "show-alias" });

        public bool ShowAllColumns => CommandParseResult.HasAppliedOption(new[] { CommandName, "columns-all" });

        public string TagFilter => CommandParseResult.GetArgumentValueAtPath(new[] { CommandName, "tag" });

        public string TemplateName { get; private set; }

        public IReadOnlyList<string>? ToInstallList => CommandParseResult.GetArgumentListAtPath(new[] { CommandName, "install" })?.ToArray();

        public IReadOnlyList<string> Tokens
        {
            get
            {
                if (CommandParseResult == null)
                {
                    return new List<string>();
                }

                return CommandParseResult.Tokens.ToList();
            }
        }

        public IReadOnlyList<string>? ToUninstallList => CommandParseResult.GetArgumentListAtPath(new[] { CommandName, "uninstall" })?.ToArray();

        public string TypeFilter => CommandParseResult.GetArgumentValueAtPath(new[] { CommandName, "type" });

        public string? SearchNameCriteria
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(TemplateName))
                {
                    return TemplateName;
                }
                return CommandParseResult.GetArgumentValueAtPath(new[] { CommandName, "search" });
            }
        }

        public string? ListNameCriteria
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(TemplateName))
                {
                    return TemplateName;
                }
                return CommandParseResult.GetArgumentValueAtPath(new[] { CommandName, "list" });
            }
        }

        protected ParseResult CommandParseResult { get; private set; }

        public bool ValidateParseError()
        {
            bool hasError = false;
            string? listOptionValueRaw = CommandParseResult.GetArgumentValueAtPath(new[] { CommandName, "list" });
            string? searchOptionValueRaw = CommandParseResult.GetArgumentValueAtPath(new[] { CommandName, "search" });

            if (IsListFlagSpecified
                && !string.IsNullOrWhiteSpace(TemplateName)
                && !string.IsNullOrWhiteSpace(listOptionValueRaw))
            {
                hasError = true;
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.InvalidSyntax, $"dotnet {CommandName} --list [PARTIAL_NAME] [FILTER_OPTIONS]"));
            }
            if (IsSearchFlagSpecified
                  && !string.IsNullOrWhiteSpace(TemplateName)
                  && !string.IsNullOrWhiteSpace(searchOptionValueRaw))
            {
                hasError = true;
                Reporter.Error.WriteLine(string.Format(LocalizableStrings.InvalidSyntax, $"dotnet {CommandName} --search [PARTIAL_NAME] [FILTER_OPTIONS]"));
            }

            if (HasColumnsParseError)
            {
                hasError = true;
                Reporter.Error.WriteLine(ColumnsParseError.Bold().Red());
            }

            IEnumerable<string> invalidParameters = RemainingParameters;
            // template options are allowed for --list, --search or when template name is specified.
            // in base parse they are treated as non-parsed arguments, so validation should fail only when invalid combination of parameters is specified.
            if (IsSearchFlagSpecified || IsListFlagSpecified || !string.IsNullOrWhiteSpace(TemplateName))
            {
                invalidParameters = GetInvalidTemplateOptionsFromRemainingParameters();
            }

            // TODO: get a meaningful error message from the parser
            if (invalidParameters.Any())
            {
                hasError = true;
                Reporter.Error.WriteLine(LocalizableStrings.InvalidCommandOptions.Bold().Red());
                foreach (string flag in invalidParameters)
                {
                    Reporter.Error.WriteLine($"  {flag}".Bold().Red());
                }
            }

            //if other error, use command parser errors (not localized)
            if (!hasError && Errors.Any())
            {
                hasError = true;
                Reporter.Error.WriteLine(LocalizableStrings.InvalidCommandOptions.Bold().Red());
                foreach (string error in Errors)
                {
                    Reporter.Error.WriteLine($"  {error}".Bold().Red());
                }
                Reporter.Error.WriteLine();
            }

            if (hasError)
            {
                if (IsHelpFlagSpecified)
                {
                    Reporter.Output.WriteLine(HelpText);
                    Reporter.Output.WriteLine();
                }
                else
                {
                    Reporter.Error.WriteLine(LocalizableStrings.RunHelpForInformationAboutAcceptedParameters);
                    Reporter.Error.WriteCommand(CommandExamples.HelpCommandExample(this.CommandName));
                }
            }
            return !hasError;
        }

        public bool HasDebuggingFlag(string flag)
        {
            return CommandParseResult.HasAppliedOption(new[] { CommandName, flag });
        }

        internal static INewCommandInput Parse(string[] args, string commandName)
        {
            List<string> argList = args.ToList();
            Command command = CommandParserSupport.CreateNewCommandWithoutTemplateInfo(commandName);
            BaseCommandInput commandInput = ParseArgs(commandName, argList, command);

            bool expansionDone = false;
            if (commandInput.ExtraArgsFileNames != null && commandInput.ExtraArgsFileNames.Count > 0)
            {
                // add the extra args to the _args and force a reparse
                // This cannot adjust the template name, so no need to re-check here.
                IReadOnlyList<string> extraArgs = AppExtensions.CreateArgListFromAdditionalFiles(commandInput.ExtraArgsFileNames);
                argList = RemoveExtraArgsTokens(commandInput, argList);
                argList.AddRange(extraArgs);
                commandInput = ParseArgs(commandName, argList, command);
                expansionDone = true;
            }

            if (string.IsNullOrWhiteSpace(commandInput.TemplateName))
            {
                bool treatUnmatchedTokensAsErrors = !(commandInput.IsListFlagSpecified || commandInput.IsSearchFlagSpecified);
                command = CommandParserSupport.CreateNewCommandForNoTemplateName(commandName, treatUnmatchedTokensAsErrors);
                commandInput = ParseArgs(commandName, argList, command);
            }
            commandInput.ExpandedExtraArgsFiles = expansionDone;
            return commandInput;
        }

        private static BaseCommandInput ParseArgs(string commandName, IReadOnlyList<string> args, Command command)
        {
            List<string> argsWithCommand = new List<string>() { commandName };
            argsWithCommand.AddRange(args);
            ParserConfiguration parseConfig = new ParserConfiguration(new Option[] { command }, argumentDelimiters: new[] { '=' }, allowUnbundling: false);
            Parser parser = new Parser(parseConfig);
            var parseResult = parser.Parse(argsWithCommand.ToArray());

            IReadOnlyCollection<string> templateNameList = parseResult.GetArgumentListAtPath(new[] { commandName });
            string? firstTemplateName = templateNameList?.FirstOrDefault();

            if (firstTemplateName != null &&
                !firstTemplateName.StartsWith("-", StringComparison.Ordinal)
                && (parseResult.Tokens.Count >= 2)
                && string.Equals(firstTemplateName, parseResult.Tokens.ElementAt(1), StringComparison.Ordinal))
            {
                return new BaseCommandInput(commandName, parseResult, firstTemplateName);
            }
            else
            {
                return new BaseCommandInput(commandName, parseResult, string.Empty);
            }
        }

        private static List<string> RemoveExtraArgsTokens(INewCommandInput commandInput, IReadOnlyList<string> originalArgs)
        {
            List<string> modifiedArgs = new List<string>();
            bool inExtraArgsContext = false;

            foreach (string token in originalArgs)
            {
                if (string.Equals(token, "-x", StringComparison.Ordinal) || string.Equals(token, "--extra-args", StringComparison.Ordinal))
                {
                    inExtraArgsContext = true;
                }
                else if (inExtraArgsContext && (commandInput.ExtraArgsFileNames?.Contains(token, StringComparer.Ordinal) ?? false))
                {
                    // Do nothing (there can be multiple extra args files).
                    // inExtraArgsContext guards against the slim possibility of a different arg having the same value as an args filename.
                    // There can be multiple extra args files - finding one doesn't change the state of things.
                }
                else
                {
                    modifiedArgs.Add(token);
                    inExtraArgsContext = false;
                }
            }

            return modifiedArgs;
        }

        private IEnumerable<string> GetInvalidTemplateOptionsFromRemainingParameters()
        {
            int i = 0;
            while (i < RemainingParameters.Count)
            {
                if (RemainingParameters[i].StartsWith("-"))
                {
                    int j = i + 1;
                    while (j < RemainingParameters.Count && !RemainingParameters[j].StartsWith("-"))
                    {
                        if (j > i + 1)
                        {
                            yield return RemainingParameters[j];
                        }
                        j++;
                    }
                    i = j;
                }
                else
                {
                    yield return RemainingParameters[i];
                    i++;
                }
            }
        }
    }
}
