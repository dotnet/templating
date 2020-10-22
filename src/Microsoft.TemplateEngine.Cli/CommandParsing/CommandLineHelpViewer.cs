using Microsoft.DotNet.Cli.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.TemplateEngine.Cli.CommandParsing
{
    internal static class CommandLineHelpViewer
    {
        private static int columnGutterWidth = 3;
        private const string templateArgument = "TEMPLATE";
        internal static string HelpView(this Command newCommand)
        {
            var helpView = new StringBuilder();
            WriteSynopsys(helpView, newCommand);
            WriteArgumentsSection(helpView);
            WriteOptionsSection(newCommand, helpView);
            return helpView.ToString();
        }

        private static void WriteSynopsys(StringBuilder helpView, Command command)
        {
            helpView.AppendLine(string.Format(LocalizableStrings.CommandUsage, command.Name, templateArgument));
            helpView.AppendLine();
        }

        private static void WriteArgumentsSection(StringBuilder helpView)
        {
            helpView.AppendLine(LocalizableStrings.ArgumentsSectionTitle);
            WriteColumnizedSummary($"  <{templateArgument}>", string.Format(LocalizableStrings.ArgumentDescriptionTemplate, templateArgument), 20, helpView);
            helpView.AppendLine();
        }

        private static void WriteOptionsSection(
            Command command,
            StringBuilder helpView)
        {
            var options = command
                .DefinedOptions
                .Where(o => !o.IsHidden())
                .ToArray();

            if (!options.Any())
            {
                return;
            }

            helpView.AppendLine(LocalizableStrings.OptionsSectionTitle);

            WriteOptionsList(options, helpView);
        }


        private static void WriteOptionsList(
            Option[] options,
            StringBuilder helpView)
        {
            var leftColumnTextFor = options
                .ToDictionary(o => o, LeftColumnText);

            var leftColumnWidth = leftColumnTextFor
                                      .Values
                                      .Select(s => s.Length)
                                      .OrderBy(length => length)
                                      .Last() + columnGutterWidth;

            foreach (var option in options)
            {
                WriteColumnizedSummary(leftColumnTextFor[option],
                                       option.HelpText,
                                       leftColumnWidth,
                                       helpView);
            }
        }

        private static string LeftColumnText(Option option)
        {
            var leftColumnText = "  " +
                                 string.Join(", ",
                                             option.RawAliases
                                                   .OrderBy(a => a.Length));

            var argumentName = option.ArgumentsRule.Name;

            if (!string.IsNullOrWhiteSpace(argumentName))
            {
                leftColumnText += $" <{argumentName}>";
            }
            return leftColumnText;
        }

        private static void WriteColumnizedSummary(
            string leftColumnText,
            string rightColumnText,
            int width,
            StringBuilder helpView)
        {
            helpView.Append(leftColumnText);

            if (leftColumnText.Length <= width - 2)
            {
                helpView.Append(new string(' ', width - leftColumnText.Length));
            }
            else
            {
                helpView.AppendLine();
                helpView.Append(new string(' ', width));
            }

            var descriptionWithLineWraps = string.Join(
                Environment.NewLine + new string(' ', width),
                rightColumnText
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim()));

            helpView.AppendLine(descriptionWithLineWraps);
        }
    }
}
