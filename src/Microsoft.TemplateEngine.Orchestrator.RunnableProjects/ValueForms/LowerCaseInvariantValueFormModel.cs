using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Json;
using Microsoft.TemplateEngine.Utils.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms
{
    public class LowerCaseInvariantValueFormModel : IValueForm
    {
        public string Identifier => "lowerCaseInvariant";

        public string Name { get; }

        public IJsonBuilder<IValueForm> JsonBuilder { get; } = new JsonBuilder<IValueForm, LowerCaseInvariantValueFormModel>(() => new LowerCaseInvariantValueFormModel());

        public LowerCaseInvariantValueFormModel()
        {
        }

        public LowerCaseInvariantValueFormModel(string name)
        {
            Name = name;
        }

        public IValueForm FromJObject(string name, JObject configuration)
        {
            return new LowerCaseInvariantValueFormModel(name);
        }

        public string Process(IReadOnlyDictionary<string, IValueForm> forms, string value)
        {
            return value.ToLowerInvariant();
        }
    }
}
