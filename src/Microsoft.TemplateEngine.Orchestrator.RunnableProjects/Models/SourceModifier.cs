using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Json;
using Microsoft.TemplateEngine.Utils.Json;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Models
{
    internal class SourceModifier : IJsonSerializable<SourceModifier>
    {
        public string Condition { get; set; }

        public List<string> Include { get; set; }

        public List<string> CopyOnly { get; set; }

        public List<string> Exclude { get; set; }

        public Dictionary<string, string> Rename { get; set; }

        public IJsonBuilder<SourceModifier> JsonBuilder { get; } = new JsonBuilder<SourceModifier, SourceModifier>(() => new SourceModifier())
            .Map(p => p.Condition, (p, v) => p.Condition = v, "Condition")
            .ListOfString().Map(p => p.Include, (p, v) => p.Include = v, () => new List<string>(), "Include")
            .ListOfString().Map(p => p.CopyOnly, (p, v) => p.CopyOnly = v, () => new List<string>(), "CopyOnly")
            .ListOfString().Map(p => p.Exclude, (p, v) => p.Exclude = v, () => new List<string>(), "Exclude")
            .DictionaryOfString().Map(p => p.Rename, (p, v) => p.Rename = v, () => new Dictionary<string, string>(StringComparer.Ordinal), "Rename")
            ;
    }
}
