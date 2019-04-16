using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Json;
using Microsoft.TemplateEngine.Utils.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms
{
    public class JsonEncodeValueFormModel : IValueForm
    {
        public string Identifier => "jsonEncode";

        public string Name { get; }

        public IJsonBuilder<IValueForm> JsonBuilder { get; } = new JsonBuilder<IValueForm, JsonEncodeValueFormModel>(() => new JsonEncodeValueFormModel());

        public JsonEncodeValueFormModel()
        {
        }

        public JsonEncodeValueFormModel(string name)
        {
            Name = name;
        }

        public IValueForm FromJObject(string name, JObject configuration)
        {
            return new JsonEncodeValueFormModel(name);
        }

        public string Process(IReadOnlyDictionary<string, IValueForm> forms, string value)
        {
            return JsonConvert.SerializeObject(value);
        }
    }
}
