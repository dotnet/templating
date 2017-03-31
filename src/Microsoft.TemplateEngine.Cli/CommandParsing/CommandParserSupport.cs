using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Settings;

namespace Microsoft.TemplateEngine.Cli.CommandParsing
{
    public class CommandParserSupport
    {
        private static IEngineEnvironmentSettings _environment;
        private static SettingsLoader _settingsLoader;

        public static void Setup(IEngineEnvironmentSettings environment)
        {
            _environment = environment;
            _settingsLoader = (SettingsLoader)_environment.SettingsLoader;
        }

        public static Command NewWithVisibleArgs(string commandName) => GetNewCommand(commandName, NewCommandVisibleArgs);

        public static Command NewWithActiveArgs(string commandName) => GetNewCommand(commandName, NewCommandVisibleArgs, NewCommandHiddenArgs, DebuggingCommandArgs);

        public static Command NewWithAllArgs(string commandName) => GetNewCommand(commandName, NewCommandVisibleArgs, NewCommandHiddenArgs, NewCommandReservedArgs, DebuggingCommandArgs);
        
        // Creates a command setup with the args for "new", plus args for the input template parameters.
        public static Command CreateNewCommandWithArgsForTemplate(string commandName, string templateName, IReadOnlyList<ITemplateParameter> parameterDefinitions, IDictionary<string, string> longNameOverrides, IDictionary<string, string> shortNameOverrides, out Dictionary<string, IList<string>> templateParamMap)
        {
            IList<Option> paramOptionList = new List<Option>();
            Option[] allBuiltInArgs = CombineArrays(NewCommandVisibleArgs, NewCommandHiddenArgs, NewCommandReservedArgs, DebuggingCommandArgs);
            HashSet<string> takenAliases = VariantsForOptions(allBuiltInArgs);
            HashSet<string> invalidParams = new HashSet<string>();
            templateParamMap = new Dictionary<string, IList<string>>();

            foreach (ITemplateParameter parameter in parameterDefinitions.Where(x => x.Priority != TemplateParameterPriority.Implicit))
            {
                string canonical = parameter.Name;
                longNameOverrides.TryGetValue(canonical, out string longOverride);
                shortNameOverrides.TryGetValue(canonical, out string shortOverride);

                if (CommandAliasAssigner.TryAssignAliasesForParameter((x) => takenAliases.Contains(x), canonical, longOverride, shortOverride, out IReadOnlyList<string> assignedAliases))
                {
                    Option option;

                    if (string.Equals(parameter.DataType, "choice", StringComparison.OrdinalIgnoreCase))
                    {
                        IList<string> choices = parameter.Choices.Keys.ToList();
                        option = Create.Option(string.Join("|", assignedAliases), parameter.Documentation,
                                            Accept.ExactlyOneArgument()
                                                //.WithSuggestionsFrom(parameter.Choices.Keys.ToArray())
                                                .With(defaultValue: () => parameter.DefaultValue));
                    }
                    else if (string.Equals(parameter.DataType, "bool", StringComparison.OrdinalIgnoreCase))
                    {
                        option = Create.Option(string.Join("|", assignedAliases), parameter.Documentation,
                                            Accept.ZeroOrOneArgument()
                                                .WithSuggestionsFrom(new[] { "true", "false" }));

                    }
                    else
                    {
                        option = Create.Option(string.Join("|", assignedAliases), parameter.Documentation,
                                            Accept.ExactlyOneArgument());
                    }

                    paramOptionList.Add(option);    // add the option
                    templateParamMap.Add(canonical, assignedAliases.ToList());   // map the template canonical name to its aliases.
                    takenAliases.UnionWith(assignedAliases);  // add the aliases to the taken aliases
                }
                else
                {
                    invalidParams.Add(canonical);
                }
            }

            if (invalidParams.Count > 0)
            {
                string unusableDisplayList = string.Join(", ", invalidParams);
                throw new Exception($"Template is malformed. The following parameter names are invalid: {unusableDisplayList}");
            }

            return GetNewCommandForTemplate(commandName, templateName, NewCommandVisibleArgs, NewCommandHiddenArgs, DebuggingCommandArgs, paramOptionList.ToArray());
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

        private static T[] CombineArrays<T>(params T[][] arrayList)
        {
            int combinedLength = 0;
            foreach (T[] arg in arrayList)
            {
                combinedLength += arg.Length;
            }

            T[] combinedArray = new T[combinedLength];
            int nextIndex = 0;
            foreach (T[] arg in arrayList)
            {
                Array.Copy(arg, 0, combinedArray, nextIndex, arg.Length);
                nextIndex += arg.Length;
            }

            return combinedArray;
        }
        
        private static Command GetNewCommandForTemplate(string commandName, string templateName, params Option[][] args)
        {
            Option[] combinedArgs = CombineArrays(args);

            return Create.Command(commandName,
                           "Initialize .NET projects.",
                           Accept.ExactlyOneArgument().WithSuggestionsFrom(templateName),
                           combinedArgs);
        }

        private static Command GetNewCommand(string commandName, params Option[][] args)
        {
            Option[] combinedArgs = CombineArrays(args);

            return Create.Command(commandName,
                           "Initialize .NET projects.",
                           Accept.ZeroOrMoreArguments(),    // this can't be ZeroOrOneArguments() because template args would cause errors
                           combinedArgs);
        }

        private static Option[] NewCommandVisibleArgs
        {
            get
            {
                return new[]
                {
                    Create.Option("-l|--list", LocalizableStrings.ListsTemplates, Accept.NoArguments()),
                    Create.Option("-lang|--language", LocalizableStrings.LanguageParameter,
                                    // TODO: dynamically get the language set for all installed templates.
                                    Accept.ExactlyOneArgument()
                                        .WithSuggestionsFrom("C#", "F#")
                                        .With(defaultValue: () => "C#")),
                    Create.Option("-n|--name", LocalizableStrings.NameOfOutput, Accept.ExactlyOneArgument()),
                    Create.Option("-o|--output", LocalizableStrings.OutputPath, Accept.ExactlyOneArgument()),
                    Create.Option("-h|--help", LocalizableStrings.DisplaysHelp, Accept.NoArguments()),
                    Create.Option("--type", LocalizableStrings.ShowsFilteredTemplates, Accept.ExactlyOneArgument()),
                    Create.Option("--force", LocalizableStrings.ForcesTemplateCreation, Accept.NoArguments()),
                };
            }
        }

        private static Option[] NewCommandHiddenArgs
        {
            get
            {
                return new[]
                {
                    Create.Option("-a|--alias", string.Empty, Accept.ExactlyOneArgument()),
                    Create.Option("-x|--extra-args", string.Empty, Accept.OneOrMoreArguments()),
                    Create.Option("--locale", string.Empty, Accept.ExactlyOneArgument()),
                    Create.Option("--quiet", string.Empty, Accept.NoArguments()),
                    Create.Option("-i|--install", string.Empty, Accept.OneOrMoreArguments()),
                    Create.Option("-all|--show-all", string.Empty, Accept.NoArguments()),
                };
            }
        }

        private static Option[] NewCommandReservedArgs
        {
            get
            {
                return new[]
                {
                    Create.Option("-up|--update", string.Empty, Accept.ZeroOrOneArgument()),
                    Create.Option("-u|--uninstall", string.Empty, Accept.ZeroOrOneArgument()),
                    Create.Option("--skip-update-check", string.Empty, Accept.NoArguments()),
                    Create.Option("--allow-scripts", string.Empty, Accept.ZeroOrOneArgument()),
                };
            }
        }

        private static Option[] DebuggingCommandArgs
        {
            get
            {
                // Including these is probably better. It'll require changes in how they're accessed
                // that will cause the old parser interface to not work correctly.
                // So wait to change things until we're definitely done with the old parser.
                //
                //return new Option[0];

                return new[]
                {
                    Create.Option("--debug:attach", string.Empty, Accept.NoArguments()),
                    Create.Option("--debug:rebuildcache", string.Empty, Accept.NoArguments()),
                    Create.Option("--debug:ephemeral-hive", string.Empty, Accept.NoArguments()),
                    Create.Option("--debug:reinit", string.Empty, Accept.NoArguments()),
                    Create.Option("--debug:reset-config", string.Empty, Accept.NoArguments()),
                    Create.Option("--debug:showconfig", string.Empty, Accept.NoArguments())
                };
            }
        }
    }
}
