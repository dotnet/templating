// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Commands
{
    internal sealed class ExportCommand : ModelBoundExecutableCommand<ExportCommandArgs>
    {
        public override Command CreateCommand()
        {
            var exportCommand = new Command("export", "Given a template.config file, creates a \"localize\" directory next to it and " +
                "exports the localization files into the created directory. If the localization files already exist, the existing translations will be preserved.");
            exportCommand.AddArgument(new Argument("template-path")
            {
                Arity = ArgumentArity.OneOrMore,
                ArgumentType = typeof(string),
                Description = "Specifies the path to the template.json file. If a directory is given, template.json file will be searched in the directory." +
                " If --recursive options is specified, all the template.json files under the given directory and its subdirectories will be taken as input.",
            });
            exportCommand.AddOption(new Option("-r")
            {
                Name = "--recursive",
                Description = "When specified, subdirectories are also searched for template.json files.",
            });
            exportCommand.AddOption(new Option("-l")
            {
                Name = "--language",
                Description = "The list of languages to be supported by this template. " +
                "The following language list will be used as default if this option is omitted: " +
                "de, en, es, fr, it, ja, ko, pl, pt-BR, ru, tr, zh-Hans, zh-Hant",
                Argument = new Argument()
                {
                    Arity = ArgumentArity.OneOrMore,
                    ArgumentType = typeof(string),
                },
            });
            exportCommand.AddOption(new Option("-d")
            {
                Name = "--DryRun",
                Description = "If this option is specified, modified files will not be saved to file system. The changes will only be printed to console output.",
            });
            exportCommand.Handler = this;

            return exportCommand;
        }

        protected override void Execute(ExportCommandArgs args)
        {

        }
    }
}
