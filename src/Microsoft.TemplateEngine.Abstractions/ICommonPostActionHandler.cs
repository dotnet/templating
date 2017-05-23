using System;
using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions
{
    public interface ICommonPostActionHandler : IIdentifiedComponent
    {
        bool Process(IEngineEnvironmentSettings environment, IPostAction action, ICreationResult templateCreationResult, string outputBasePath);
    }
}
