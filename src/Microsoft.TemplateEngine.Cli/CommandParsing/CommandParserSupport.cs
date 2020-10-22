using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Cli.CommandParsing
{
    public static class CommandParserSupport
    {
        public static Command CreateNewCommandWithoutTemplateInfo(string commandName) => GetNewCommand(commandName, NewCommandVisibleArgs, NewCommandHiddenArgs, DebuggingCommandArgs);

        private static Command GetNewCommand(string commandName, params Option[][] args)
        {
            Option[] combinedArgs = ArrayExtensions.CombineArrays(args);

            return Create.Command(commandName,
                           LocalizableStrings.CommandDescription,
                           Accept.ZeroOrMoreArguments(),    // this can't be ZeroOrOneArguments() because template args would cause errors
                           combinedArgs);
        }

        // Final parser for when there is no template name provided.
        // Unmatched args are errors.
        public static Command CreateNewCommandForNoTemplateName(string commandName)
        {
            Option[] combinedArgs = ArrayExtensions.CombineArrays(NewCommandVisibleArgs, NewCommandHiddenArgs, DebuggingCommandArgs);

            return Create.Command(commandName,
                           LocalizableStrings.CommandDescription,
                           Accept.NoArguments(),
                           true,
                           combinedArgs);
        }

        private static Command GetNewCommandForTemplate(string commandName, string templateName, params Option[][] args)
        {
            Option[] combinedArgs = ArrayExtensions.CombineArrays(args);

            return Create.Command(commandName,
                           LocalizableStrings.CommandDescription,
                           Accept.ExactlyOneArgument().WithSuggestionsFrom(templateName),
                           combinedArgs);
        }

        public static HashSet<string> ArgsForBuiltInCommands
        {
            get
            {
                if (_argsForBuiltInCommands == null)
                {
                    Option[] allBuiltInArgs = ArrayExtensions.CombineArrays(NewCommandVisibleArgs, NewCommandHiddenArgs, NewCommandReservedArgs, DebuggingCommandArgs);

                    _argsForBuiltInCommands = VariantsForOptions(allBuiltInArgs);
                }

                // return a copy so the original doesn't get modified.
                return new HashSet<string>(_argsForBuiltInCommands);
            }
        }
        private static HashSet<string> _argsForBuiltInCommands = null;

        // Creates a command setup with the args for "new", plus args for the input template parameters.
        public static Command CreateNewCommandWithArgsForTemplate(string commandName, string templateName,
                    IReadOnlyList<ITemplateParameter> parameterDefinitions,
                    IDictionary<string, string> longNameOverrides,
                    IDictionary<string, string> shortNameOverrides,
                    out IReadOnlyDictionary<string, IReadOnlyList<string>> templateParamMap)
        {
            IList<Option> paramOptionList = new List<Option>();
            HashSet<string> initiallyTakenAliases = ArgsForBuiltInCommands;

            Dictionary<string, IReadOnlyList<string>> canonicalToVariantMap = new Dictionary<string, IReadOnlyList<string>>();
            AliasAssignmentCoordinator assignmentCoordinator = new AliasAssignmentCoordinator(parameterDefinitions, longNameOverrides, shortNameOverrides, initiallyTakenAliases);

            if (assignmentCoordinator.InvalidParams.Count > 0)
            {
                string unusableDisplayList = string.Join(", ", assignmentCoordinator.InvalidParams);
                throw new Exception($"Template is malformed. The following parameter names are invalid: {unusableDisplayList}");
            }

            foreach (ITemplateParameter parameter in parameterDefinitions.Where(x => x.Priority != TemplateParameterPriority.Implicit))
            {
                Option option;
                IList<string> aliasesForParam = new List<string>();

                if (assignmentCoordinator.LongNameAssignments.TryGetValue(parameter.Name, out string longVersion))
                {
                    aliasesForParam.Add(longVersion);
                }

                if (assignmentCoordinator.ShortNameAssignments.TryGetValue(parameter.Name, out string shortVersion))
                {
                    aliasesForParam.Add(shortVersion);
                }

                if (parameter is IAllowDefaultIfOptionWithoutValue parameterWithNoValueDefault
                    && !string.IsNullOrEmpty(parameterWithNoValueDefault.DefaultIfOptionWithoutValue))
                {
                    // This switch can be provided with or without a value.
                    // If the user doesn't specify a value, it gets the value of DefaultIfOptionWithoutValue
                    option = Create.Option(string.Join("|", aliasesForParam), parameter.Documentation,
                                            Accept.ZeroOrOneArgument());
                }
                else
                {
                    // User must provide a value if this switch is specified.
                    option = Create.Option(string.Join("|", aliasesForParam), parameter.Documentation,
                                        Accept.ExactlyOneArgument());
                }

                paramOptionList.Add(option);    // add the option
                canonicalToVariantMap.Add(parameter.Name, aliasesForParam.ToList());   // map the template canonical name to its aliases.
            }

            templateParamMap = canonicalToVariantMap;
            return GetNewCommandForTemplate(commandName, templateName, NewCommandVisibleArgs, NewCommandHiddenArgs, DebuggingCommandArgs, paramOptionList.ToArray());
        }

        private static Option[] NewCommandVisibleArgs
        {
            get
            {
                return new[]
                {
                    Create.Option(
                        "-n|--name",
                        LocalizableStrings.OptionDescriptionName,
                        Accept.ExactlyOneArgument().With(LocalizableStrings.OptionDescriptionName, "OUTPUT_NAME")),
                    Create.Option(
                        "-o|--output",
                        LocalizableStrings.OptionDescriptionOutput,
                        Accept.ExactlyOneArgument().With(LocalizableStrings.OptionDescriptionOutput, "OUTPUT_DIRECTORY")),
                    Create.Option("--interactive", LocalizableStrings.OptionDescriptionInteractive, Accept.NoArguments()),
                    Create.Option("--dry-run", LocalizableStrings.OptionDescriptionDryrun, Accept.NoArguments()),
                    Create.Option("--force", LocalizableStrings.OptionDescriptionForce, Accept.NoArguments()),
                    Create.Option(
                        "-lang|--language",
                        LocalizableStrings.OptionDescriptionLanguage,
                        Accept
                            .ExactlyOneArgument()
                            .WithSuggestionsFrom("C#", "F#", "VB")
                            .With(LocalizableStrings.OptionDescriptionLanguage, "LANGUAGE")),
                    Create.Option(
                        "--type",
                        LocalizableStrings.OptionDescriptionTypeFilter,
                        Accept.ExactlyOneArgument().With(LocalizableStrings.OptionDescriptionTypeFilter,"TYPE")),
                    Create.Option("-h|--help", LocalizableStrings.OptionDescriptionHelp, Accept.NoArguments()),
                    Create.Option("-l|--list", LocalizableStrings.OptionDescriptionList, Accept.NoArguments()),
                    Create.Option(
                        "-i|--install",
                        LocalizableStrings.OptionDescriptionInstall,
                        Accept.OneOrMoreArguments().With(LocalizableStrings.OptionDescriptionInstall,"PATH|NUGET_ID")),
                    Create.Option(
                        "--nuget-source",
                        LocalizableStrings.OptionDescriptionNugetSource,
                        Accept.OneOrMoreArguments().With(LocalizableStrings.OptionDescriptionNugetSource,"SOURCE")),
                    Create.Option(
                        "-u|--uninstall",
                        LocalizableStrings.OptionDescriptionUninstall,
                        Accept.ZeroOrMoreArguments().With(LocalizableStrings.OptionDescriptionUninstall,"PATH|NUGET_ID")),
                    Create.Option("--update-check", LocalizableStrings.OptionDescriptionUpdateCheck, Accept.NoArguments()),
                    Create.Option("--update-apply", LocalizableStrings.OptionDescriptionUpdateApply, Accept.NoArguments()),
                    Create.Option("--columns", LocalizableStrings.OptionDescriptionColumns, Accept.ExactlyOneArgument().With(LocalizableStrings.OptionDescriptionColumns, "COLUMNS_LIST")),
                    Create.Option("--columns-all", LocalizableStrings.OptionDescriptionColumnsAll, Accept.NoArguments()),
                    Create.Option("--author", LocalizableStrings.OptionDescriptionAuthorFilter, Accept.ExactlyOneArgument().With(LocalizableStrings.OptionDescriptionColumns, "AUTHOR"))
                };
            }
        }

        private static Option[] NewCommandHiddenArgs
        {
            get
            {
                return new[]
                {
                    //Create.Option("-a|--alias", LocalizableStrings.AliasHelp, Accept.ExactlyOneArgument()),
                    //Create.Option("--show-alias", LocalizableStrings.ShowAliasesHelp, Accept.ZeroOrOneArgument()),
                    // When these are un-hidden, be sure to set their help values like above.
                    Create.Option("-a|--alias", string.Empty, Accept.ExactlyOneArgument()),
                    Create.Option("--show-alias", string.Empty, Accept.ZeroOrOneArgument()),
                    Create.Option("-x|--extra-args", string.Empty, Accept.OneOrMoreArguments()),
                    Create.Option("--locale", string.Empty, Accept.ExactlyOneArgument()),
                    Create.Option("--quiet", string.Empty, Accept.NoArguments()),
                    Create.Option("-all|--show-all", string.Empty, Accept.NoArguments()),
                    Create.Option("--allow-scripts", string.Empty, Accept.ZeroOrOneArgument()),
                    Create.Option("--baseline", string.Empty, Accept.ExactlyOneArgument()),
                };
            }
        }

        private static Option[] NewCommandReservedArgs
        {
            get
            {
                return new[]
                {
                    Create.Option("--skip-update-check", string.Empty, Accept.NoArguments()),
                };
            }
        }

        private static Option[] DebuggingCommandArgs
        {
            get
            {
                return new[]
                {
                    Create.Option("--debug:attach", string.Empty, Accept.NoArguments()),
                    Create.Option("--debug:rebuildcache", string.Empty, Accept.NoArguments()),
                    Create.Option("--debug:ephemeral-hive", string.Empty, Accept.NoArguments()),
                    Create.Option("--debug:reinit", string.Empty, Accept.NoArguments()),
                    Create.Option("--debug:reset-config", string.Empty, Accept.NoArguments()),
                    Create.Option("--debug:showconfig", string.Empty, Accept.NoArguments()),
                    Create.Option("--debug:emit-timings", string.Empty, Accept.NoArguments()),
                    Create.Option("--debug:emit-telemetry", string.Empty, Accept.NoArguments()),
                    Create.Option("--debug:custom-hive", string.Empty, Accept.ExactlyOneArgument()),
                    Create.Option("--debug:version", string.Empty, Accept.NoArguments()),

                    Create.Option("--dev:install", string.Empty, Accept.NoArguments()),

                    Create.Option("--trace:authoring", string.Empty, Accept.NoArguments()),
                    Create.Option("--trace:install", string.Empty, Accept.NoArguments()),
                };
            }
        }

        private static HashSet<string> VariantsForOptions(Option[] options)
        {
            HashSet<string> variants = new HashSet<string>();

            if (options == null)
            {
                return variants;
            }

            for (int i = 0; i < options.Length; i++)
            {
                variants.UnionWith(options[i].RawAliases);
            }

            return variants;
        }
    }
}
