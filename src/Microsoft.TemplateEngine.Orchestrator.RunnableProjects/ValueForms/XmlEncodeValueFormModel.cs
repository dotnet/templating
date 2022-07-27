// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ConfigModel;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms
{
    internal class XmlEncodeValueFormModel : ISerializableValueForm
    {
        private static readonly XmlWriterSettings Settings = new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment };

        internal XmlEncodeValueFormModel()
        {
        }

        internal XmlEncodeValueFormModel(string name)
        {
            Name = name;
        }

        public string Identifier => "xmlEncode";

        public string Name { get; }

        public IValueForm FromJObject(string name, JObject configuration)
        {
            return new XmlEncodeValueFormModel(name);
        }

        public string Process(string value, IReadOnlyDictionary<string, IValueForm> forms)
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
