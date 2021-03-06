// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms
{
    internal interface IValueForm
    {
        string Identifier { get; }

        string Name { get; }

        string Process(IReadOnlyDictionary<string, IValueForm> forms, string value);

        IValueForm FromJObject(string name, JObject configuration);
    }
}
