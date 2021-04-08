using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms
{
    internal class TitleCaseValueFormModel : IValueForm
    {
        public string Identifier => "titleCase";

        public string Name { get; }

        internal TitleCaseValueFormModel()
        {
        }

        internal TitleCaseValueFormModel(string name)
        {
            Name = name;
        }

        public IValueForm FromJObject(string name, JObject configuration)
        {
            return new TitleCaseValueFormModel(name);
        }

        public string Process(IReadOnlyDictionary<string, IValueForm> forms, string value)
        {
            switch (value)
            {
                case null: return null;
                case "": return value;
                default: return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value);
            }
        }
    }
}
