﻿using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli.PostActionProcessors
{
    public interface IPostActionProcessor : IIdentifiedComponent
    {
        bool Process(IEngineEnvironmentSettings settings, IPostAction action, ICreationResult templateCreationResult, string outputBasePath);
    }
}
