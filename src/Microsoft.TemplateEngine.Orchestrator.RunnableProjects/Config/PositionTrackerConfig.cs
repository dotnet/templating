using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Core;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Core.Operations;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Config
{
    public class PositionTrackerConfig : IOperationConfig
    {
        public Guid Id { get; } = new Guid("EEF8CAEE-0605-4929-8478-550FFDCE6E7E");

        public string Key => PositionTracker.OperationName;

        public IEnumerable<IOperationProvider> ConfigureFromJObject(JObject rawConfiguration, IDirectory templateRoot)
        {
            string afterToken = rawConfiguration.ToString("afterToken");
            string beforeToken = rawConfiguration.ToString("beforeToken");
            string token = rawConfiguration.ToString("token");
            string id = rawConfiguration.ToString("id");
            bool isInitialStateOn = rawConfiguration.ToBool("isInitialStateOn", true);

            TokenConfig tokenConfig = TokenConfig.FromValue(token);

            if (!string.IsNullOrEmpty(afterToken))
            {
                tokenConfig = tokenConfig.OnlyIfAfter(afterToken);
            }

            if (!string.IsNullOrEmpty(beforeToken))
            {
                tokenConfig = tokenConfig.OnlyIfBefore(beforeToken);
            }

            yield return new PositionTracker(id, tokenConfig, isInitialStateOn);
        }
    }
}
