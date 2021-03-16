// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.TemplateLocalizer.Commands;
using Microsoft.TemplateEngine.TemplateLocalizer.Commands.Export;

namespace Microsoft.TemplateEngine.TemplateLocalizer
{
    internal sealed class Program
    {
        private static readonly ExecutableCommand[] commands = new[]
        {
            new ExportCommand(),
        };

        private static async Task<int> Main(string[] args)
        {
            ILogger logger = InitializeLogger();

            var rootCommand = new RootCommand();
            rootCommand.Name = "dotnet-template-localizer";

            foreach(ExecutableCommand command in commands)
            {
                command.Logger = logger;
                rootCommand.AddCommand(command.CreateCommand());
            }

            return await rootCommand.InvokeAsync(args);
        }

        private static ILogger InitializeLogger()
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            return loggerFactory.CreateLogger<Program>();
        }
    }
}
