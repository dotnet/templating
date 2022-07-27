// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ConfigModel;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms
{
    internal class FirstUpperCaseValueFormModel : ISerializableValueForm
    {
        internal FirstUpperCaseValueFormModel()
        {
        }

        internal FirstUpperCaseValueFormModel(string name)
        {
            Name = name;
        }

        public string Identifier => "firstUpperCase";

        public string Name { get; }

        public IValueForm FromJObject(string name, JObject configuration)
        {
            return new FirstUpperCaseValueFormModel(name);
        }

        public string Process(string value, IReadOnlyDictionary<string, IValueForm> forms)
        {
            switch (value)
            {
                case null: return null;
                case "": return value;
                default: return value.First().ToString().ToUpper() + value.Substring(1);
            }
        }
    }
}
