using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli.PostActionProcessors
{
    public class SortUsingsPostAction : IPostActionProcessor
    {
        public static readonly Guid ActionProcessorId = new Guid("F65519F3-E665-467E-9734-A962AEB25FC6");

        public Guid Id => ActionProcessorId;

        public bool Process(IEngineEnvironmentSettings settings, IPostAction actionConfig, ICreationResult templateCreationResult, string outputBasePath)
        {
            if (!settings.SettingsLoader.Components.TryGetComponent(new Guid("8A0FF3C4-CE1F-4958-B0AB-C94D40E36EED"), out ICommonPostActionHandler implementation))
            {
                return false;
            }

            return implementation.Process(settings, actionConfig, templateCreationResult, outputBasePath);
        }
    }
}
