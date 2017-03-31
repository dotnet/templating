using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.TemplateEngine.Cli.CommandParsing
{
    public static class ExtendedCommandParserSupport
    {
        public static ExtendedCommandParserNewCommandInput SetupParser(string commandName)
        {
            ExtendedCommandParser app = new ExtendedCommandParser()
            {
                Name = $"dotnet {commandName}",
                FullName = LocalizableStrings.CommandDescription
            };
            SetupInternalCommands(app);

            return new ExtendedCommandParserNewCommandInput(app, ExtendedCommandParserResetter);
        }

        public static void ExtendedCommandParserResetter(ExtendedCommandParser parser)
        {
            parser.Reset();
            SetupInternalCommands(parser);
        }

        public static void SetupInternalCommands(ExtendedCommandParser appExt)
        {
            // visible
            appExt.InternalOption("-l|--list", "--list", LocalizableStrings.ListsTemplates, CommandOptionType.NoValue);
            appExt.InternalOption("-lang|--language", "--language", LocalizableStrings.LanguageParameter, CommandOptionType.SingleValue);
            appExt.InternalOption("-n|--name", "--name", LocalizableStrings.NameOfOutput, CommandOptionType.SingleValue);
            appExt.InternalOption("-o|--output", "--output", LocalizableStrings.OutputPath, CommandOptionType.SingleValue);
            appExt.InternalOption("-h|--help", "--help", LocalizableStrings.DisplaysHelp, CommandOptionType.NoValue);
            appExt.InternalOption("--type", "--type", LocalizableStrings.ShowsFilteredTemplates, CommandOptionType.SingleValue);
            appExt.InternalOption("--force", "--force", LocalizableStrings.ForcesTemplateCreation, CommandOptionType.NoValue);
            appExt.InternalOption("--allow-scripts", "--allow-scripts", LocalizableStrings.WhetherToAllowScriptsToRun, CommandOptionType.SingleValue);

            // hidden
            appExt.HiddenInternalOption("-a|--alias", "--alias", CommandOptionType.SingleValue);
            appExt.HiddenInternalOption("-x|--extra-args", "--extra-args", CommandOptionType.MultipleValue);
            appExt.HiddenInternalOption("--locale", "--locale", CommandOptionType.SingleValue);
            appExt.HiddenInternalOption("--quiet", "--quiet", CommandOptionType.NoValue);
            appExt.HiddenInternalOption("-i|--install", "--install", CommandOptionType.MultipleValue);
            appExt.HiddenInternalOption("-all|--show-all", "--show-all", CommandOptionType.NoValue);

            // reserved but not currently used
            appExt.HiddenInternalOption("-up|--update", "--update", CommandOptionType.MultipleValue);
            appExt.HiddenInternalOption("-u|--uninstall", "--uninstall", CommandOptionType.MultipleValue);
            appExt.HiddenInternalOption("--skip-update-check", "--skip-update-check", CommandOptionType.NoValue);
        }
    }
}
