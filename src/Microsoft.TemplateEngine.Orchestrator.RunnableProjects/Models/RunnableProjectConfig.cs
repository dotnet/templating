using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Json;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms;
using Microsoft.TemplateEngine.Utils.Json;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Models
{
    internal class RunnableProjectConfig : IJsonSerializable<RunnableProjectConfig>
    {
        public string Author { get; set; }

        public List<string> Classifications { get; set; }

        public string DefaultName { get; set; }

        public string Description { get; set; }

        public string GroupIdentity { get; set; }

        public int Precedence { get; set; }

        public List<Guid> Guids { get; set; }

        public string Identity { get; set; }

        public string Name { get; set; }

        public string SourceName { get; set; }

        public string PlaceholderFilename { get; set; }

        public string GeneratorVersions { get; set; }

        public List<string> ShortName { get; set; }

        public Dictionary<string, IValueForm> Forms { get; set; }

        public List<ExtendedFileSource> Sources { get; set; }

        public Dictionary<string, BaselineInfo> Baselines { get; set; }

        public Dictionary<string, string> Tags { get; set; }

        public IJsonBuilder<RunnableProjectConfig> JsonBuilder { get; } = new JsonBuilder<RunnableProjectConfig, RunnableProjectConfig>(() => new RunnableProjectConfig())
            .Map(p => p.Author, (p, v) => p.Author = v, "Author")
            .ListOfString().Map(p => p.Classifications, (p, v) => p.Classifications = v, () => new List<string>(), "Classifications")
            .Map(p => p.DefaultName, (p, v) => p.DefaultName = v, "DefaultName")
            .Map(p => p.Description, (p, v) => p.Description = v, "Description")
            .Map(p => p.GroupIdentity, (p, v) => p.GroupIdentity = v, "GroupIdentity")
            .Map(p => p.Precedence, (p, v) => p.Precedence = v, "Precedence")
            .ListOfGuid().Map(p => p.Guids, (p, v) => p.Guids = v, () => new List<Guid>(), "Guids")
            .Map(p => p.Identity, (p, v) => p.Identity = v, "Identity")
            .Map(p => p.Name, (p, v) => p.Name = v, "Name")
            .Map(p => p.SourceName, (p, v) => p.SourceName = v, "SourceName")
            .Map(p => p.PlaceholderFilename, (p, v) => p.PlaceholderFilename = v, "PlaceholderFilename")
            .Map(p => p.GeneratorVersions, (p, v) => p.GeneratorVersions = v, "GeneratorVersions")
            .ListOfString().Map(p => p.ShortName, (p, v) => p.ShortName = v, () => new List<string>(), "ShortName")
            .Dictionary<RunnableProjectConfig, IValueForm>(b => b.Map(p => p.Value, (p, v) => p.Value = v, ValueFormRegistry.GetSelectorMappings, "Value")).Map(p => p.Forms, (p, v) => p.Forms = v, () => new Dictionary<string, IValueForm>(StringComparer.Ordinal), "Forms")
            .List<RunnableProjectConfig, ExtendedFileSource>(b => b.Map(p => p.Value, (p, v) => p.Value = v, "Value")).Map(p => p.Sources, (p, v) => p.Sources = v, () => new List<ExtendedFileSource>(), "Sources")
            .Dictionary<RunnableProjectConfig, BaselineInfo>(b => b.Map(p => p.Value, (p, v) => p.Value = v, "Value")).Map(p => p.Baselines, (p, v) => p.Baselines = v, () => new Dictionary<string, BaselineInfo>(StringComparer.Ordinal), "Baselines")
            .DictionaryOfString().Map(p => p.Tags, (p, v) => p.Tags = v, () => new Dictionary<string, string>(StringComparer.Ordinal), "Tags")
            ;
    }
}
