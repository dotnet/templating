using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Json;
using Microsoft.TemplateEngine.Utils.Json;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Models
{
    internal class BaselineInfo : IJsonSerializable<BaselineInfo>
    {
        public string Description { get; set; }

        public Dictionary<string, string> DefaultOverrides { get; set; }

        public IJsonBuilder<BaselineInfo> JsonBuilder { get; } = new JsonBuilder<BaselineInfo, BaselineInfo>(() => new BaselineInfo())
            .Map(p => p.Description, (p, v) => p.Description = v, "Description")
            .DictionaryOfString().Map(p => p.DefaultOverrides, (p, v) => p.DefaultOverrides = v, () => new Dictionary<string, string>(StringComparer.Ordinal), "DefaultOverrides")
            ;
    }
}
