// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ConfigModel;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms
{
    internal class IdentityValueForm : ISerializableValueForm
    {
        internal const string FormName = "identity";

        internal IdentityValueForm()
        {
        }

        internal IdentityValueForm(string name)
        {
            Name = name;
        }

        public string Identifier => FormName;

        public string Name { get; }

        public IValueForm FromJObject(string name, JObject configuration)
        {
            return new IdentityValueForm(name);
        }

        public string Process(string value, IReadOnlyDictionary<string, IValueForm> forms)
        {
            return value;
        }
    }
}
