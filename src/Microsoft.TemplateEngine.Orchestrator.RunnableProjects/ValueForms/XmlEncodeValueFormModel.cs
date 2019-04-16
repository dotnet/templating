using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.TemplateEngine.Abstractions.Json;
using Microsoft.TemplateEngine.Utils.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms
{
    public class XmlEncodeValueFormModel : IValueForm
    {
        private static readonly XmlWriterSettings Settings = new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment };

        public string Identifier => "xmlEncode";

        public string Name { get; }

        public XmlEncodeValueFormModel()
        {
        }

        public XmlEncodeValueFormModel(string name)
        {
            Name = name;
        }

        public IJsonBuilder<IValueForm> JsonBuilder { get; } = new JsonBuilder<IValueForm, XmlEncodeValueFormModel>(() => new XmlEncodeValueFormModel());

        public IValueForm FromJObject(string name, JObject configuration)
        {
            return new XmlEncodeValueFormModel(name);
        }

        public string Process(IReadOnlyDictionary<string, IValueForm> forms, string value)
        {
            StringBuilder output = new StringBuilder();
            using (XmlWriter w = XmlWriter.Create(output, Settings))
            {
                w.WriteString(value);
            }
            return output.ToString();
        }
    }
}
