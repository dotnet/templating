using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.TemplateEngine.Abstractions.Json;
using Microsoft.TemplateEngine.Utils.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms
{
    public class ReplacementValueFormModel : IValueForm
    {
        private Regex _match;

        public ReplacementValueFormModel()
        {
        }

        public ReplacementValueFormModel(string name, string pattern, string replacement)
        {
            _match = new Regex(pattern);
            Replacement = replacement;
            Name = name;
        }

        public string Identifier => "replace";

        public string Name { get; }

        public string Pattern
        {
            get => _match.ToString();
            set => _match = new Regex(value);
        }

        public string Replacement { get; set; }

        public IJsonBuilder<IValueForm> JsonBuilder { get; } = new JsonBuilder<IValueForm, ReplacementValueFormModel>(() => new ReplacementValueFormModel())
            .Map(x => x.Pattern, (x, v) => x.Pattern = v, nameof(Pattern))
            .Map(x => x.Replacement, (x, v) => x.Replacement = v, nameof(Replacement));

        public IValueForm FromJObject(string name, JObject configuration)
        {
            return new ReplacementValueFormModel(name, configuration.ToString("pattern"), configuration.ToString("replacement"));
        }

        public string Process(IReadOnlyDictionary<string, IValueForm> forms, string value)
        {
            return _match.Replace(value, Replacement);
        }
    }
}
