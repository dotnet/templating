﻿using System;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli.PostActionProcessors
{
    public class InstructionDisplayPostActionProcessor : IPostActionProcessor
    {
        private static readonly Guid ActionProcessorId = new Guid("AC1156F7-BB77-4DB8-B28F-24EEBCCA1E5C");

        public Guid Id => ActionProcessorId;

        public InstructionDisplayPostActionProcessor()
        {
        }

        public bool Process(IEngineEnvironmentSettings settings, IPostAction actionConfig, ICreationResult templateCreationResult, string outputBasePath)
        {
            Console.WriteLine(string.Format(LocalizableStrings.PostActionDescription, actionConfig.Description));
            Console.WriteLine(string.Format(LocalizableStrings.PostActionInstructions, actionConfig.ManualInstructions));

            if (actionConfig.Args != null && actionConfig.Args.TryGetValue("executable", out string executable))
            {
                actionConfig.Args.TryGetValue("args", out string commandArgs);
                Reporter.Output.WriteLine(string.Format(LocalizableStrings.PostActionCommand, $"{executable} {commandArgs}".Bold().Red()));
            }

            return true;
        }
    }
}
