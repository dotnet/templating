using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Json;
using Microsoft.TemplateEngine.Utils.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms
{
    public class ChainValueFormModel : IValueForm
    {
        private IReadOnlyList<string> _steps;

        public ChainValueFormModel()
        {
        }

        public ChainValueFormModel(string name, IReadOnlyList<string> steps)
        {
            Name = name;
            _steps = steps;
        }

        public string Identifier => "chain";

        public string Name { get; }

        public IJsonBuilder<IValueForm> JsonBuilder { get; } = new JsonBuilder<IValueForm, ChainValueFormModel>(() => new ChainValueFormModel()).ListOfString().Map(x => x._steps, (x, v) => x._steps = v, () => new List<string>(), "steps");

        public IValueForm FromJObject(string name, JObject configuration)
        {
            return new ChainValueFormModel(name, configuration.ArrayAsStrings("steps"));
        }

        public string Process(IReadOnlyDictionary<string, IValueForm> forms, string value)
        {
            string result = value;

            foreach (string step in _steps)
            {
                result = forms[step].Process(forms, result);
            }

            return result;
        }
    }
}
