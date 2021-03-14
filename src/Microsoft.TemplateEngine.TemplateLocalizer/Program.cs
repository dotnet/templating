// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.TemplateLocalizer.Commands;

namespace Microsoft.TemplateEngine.TemplateLocalizer
{
    class Program
    {
        private static ExecutableCommand[] commands = new[]
        {
            new ExportCommand(),
        };

        private static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();
            rootCommand.Name = "dotnet-template-localizer";

            foreach(ExecutableCommand command in commands)
            {
                rootCommand.AddCommand(command.CreateCommand());
            }
            
            return await rootCommand.InvokeAsync(args);
        }
    }
}
