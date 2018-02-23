using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    public class Parameter : ITemplateParameter, IExtendedTemplateParameter, IAllowDefaultIfOptionWithoutValue
    {
        [JsonProperty]
        public string Description { get; set; }

        [JsonProperty]
        public string DefaultValue { get; set; }

        [JsonIgnore]
        public string Name { get; set; }

        [JsonProperty]
        public bool IsName { get; set; }

        [JsonProperty]
        public TemplateParameterPriority Requirement { get; set; }

        [JsonProperty]
        public string Type { get; set; }

        [JsonProperty]
        public bool IsVariable { get; set; }

        [JsonProperty]
        public string DataType { get; set; }

        [JsonProperty]
        public string DefaultIfOptionWithoutValue { get; set; }

        public bool ShouldSerializeDefaultIfOptionWithoutValue()
        {
            return !string.IsNullOrEmpty(DefaultIfOptionWithoutValue);
        }

        [JsonProperty]
        public string FileRename { get; set; }

        [JsonProperty]
        public IReadOnlyDictionary<string, string> Choices { get; set; }

        [JsonProperty]
        public IReadOnlyDictionary<string, IReadOnlyList<string>> Forms { get; set; }

        [JsonIgnore]
        public string Documentation
        {
            get { return Description; }
            set { Description = value; }
        }

        string ITemplateParameter.Name => Name;

        TemplateParameterPriority ITemplateParameter.Priority => Requirement;

        string ITemplateParameter.Type => Type;

        bool ITemplateParameter.IsName => IsName;

        string ITemplateParameter.DefaultValue => DefaultValue;

        string ITemplateParameter.DataType => DataType;

        string IAllowDefaultIfOptionWithoutValue.DefaultIfOptionWithoutValue
        {
            get
            {
                return DefaultIfOptionWithoutValue;
            }
            set
            {
                DefaultIfOptionWithoutValue = value;
            }
        }

        IReadOnlyDictionary<string, string> ITemplateParameter.Choices => Choices;

        public override string ToString()
        {
            return $"{Name} ({Type})";
        }
    }
}
