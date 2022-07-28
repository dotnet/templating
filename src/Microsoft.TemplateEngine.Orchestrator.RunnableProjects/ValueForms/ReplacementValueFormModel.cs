// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ConfigModel;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms
{
    internal class ReplacementValueFormModel : ISerializableValueForm
    {
        private readonly Regex _match;
        private readonly string _replacment;

        internal ReplacementValueFormModel()
        {
        }

        internal ReplacementValueFormModel(string name, string pattern, string replacement)
        {
            _match = new Regex(pattern);
            _replacment = replacement;
            Name = name;
        }

        public string Identifier => "replace";

        public string Name { get; }

        public IValueForm FromJObject(string name, JObject configuration)
        {
            return new ReplacementValueFormModel(name, configuration.ToString("pattern"), configuration.ToString("replacement"));
        }

        public string Process(string value, IReadOnlyDictionary<string, IValueForm> forms)
        {
            return _match.Replace(value, _replacment);
        }
    }
}
