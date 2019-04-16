using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Json;
using Microsoft.TemplateEngine.Utils.Json;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Models
{
    internal class ExtendedFileSource : IJsonSerializable<ExtendedFileSource>
    {
        public List<string> Include { get; set; }

        public List<string> CopyOnly { get; set; }

        public List<string> Exclude { get; set; }

        public Dictionary<string, string> Rename { get; set; }

        public string Source { get; set; }

        public string Target { get; set; }

        public string Condition { get; set; }

        public List<SourceModifier> Modifiers { get; set; }

        public IJsonBuilder<ExtendedFileSource> JsonBuilder { get; } = new JsonBuilder<ExtendedFileSource, ExtendedFileSource>(() => new ExtendedFileSource())
            .ListOfString().Map(p => p.Include, (p, v) => p.Include = v, () => new List<string>(), "Include")
            .ListOfString().Map(p => p.CopyOnly, (p, v) => p.CopyOnly = v, () => new List<string>(), "CopyOnly")
            .ListOfString().Map(p => p.Exclude, (p, v) => p.Exclude = v, () => new List<string>(), "Exclude")
            .DictionaryOfString().Map(p => p.Rename, (p, v) => p.Rename = v, () => new Dictionary<string, string>(StringComparer.Ordinal), "Rename")
            .Map(p => p.Source, (p, v) => p.Source = v, "Source")
            .Map(p => p.Target, (p, v) => p.Target = v, "Target")
            .Map(p => p.Condition, (p, v) => p.Condition = v, "Condition")
            .List<ExtendedFileSource, SourceModifier>(b => b.Map(p => p.Value, (p, v) => p.Value = v, "Value")).Map(p => p.Modifiers, (p, v) => p.Modifiers = v, () => new List<SourceModifier>(), "Modifiers")
            ;
    }
}
