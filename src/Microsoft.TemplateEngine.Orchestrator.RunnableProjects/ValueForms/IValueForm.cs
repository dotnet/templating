using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms
{
    public interface IValueForm : IJsonSerializable<IValueForm>
    {
        string Identifier { get; }

        string Name { get; }

        string Process(IReadOnlyDictionary<string, IValueForm> forms, string value);

        IValueForm FromJObject(string name, JObject configuration);
    }
}
