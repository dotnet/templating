// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ConfigModel;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms
{
    internal class UpperCaseValueFormModel : ISerializableValueForm
    {
        internal UpperCaseValueFormModel()
        {
        }

        internal UpperCaseValueFormModel(string name)
        {
            Name = name;
        }

        public string Identifier => "upperCase";

        public string Name { get; }

        public IValueForm FromJObject(string name, JObject configuration)
        {
            return new UpperCaseValueFormModel(name);
        }

        public string Process(string value, IReadOnlyDictionary<string, IValueForm> forms)
        {
            return value.ToUpper();
        }
    }
}
