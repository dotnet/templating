﻿using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    public class BaselineInfo : IBaselineInfo
    {
        [JsonProperty]
        public string Description { get; set; }

        [JsonProperty]
        public IReadOnlyDictionary<string, string> DefaultOverrides { get; set; }
    }
}
