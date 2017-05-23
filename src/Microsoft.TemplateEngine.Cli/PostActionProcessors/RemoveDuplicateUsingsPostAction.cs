using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli.PostActionProcessors
{
    public class RemoveDuplicateUsingsPostAction : IPostActionProcessor
    {
        public static readonly Guid ActionProcessorId = new Guid("EE2061F3-2FAA-47C9-9B40-198A9DF51C39");

        public Guid Id => ActionProcessorId;

        public bool Process(IEngineEnvironmentSettings settings, IPostAction actionConfig, ICreationResult templateCreationResult, string outputBasePath)
        {
            if (!settings.SettingsLoader.Components.TryGetComponent(new Guid("C405DDB2-4F64-4A2D-8005-1243D5C55EAD"), out ICommonPostActionHandler implementation))
            {
                return false;
            }

            return implementation.Process(settings, actionConfig, templateCreationResult, outputBasePath);
        }
    }
}
