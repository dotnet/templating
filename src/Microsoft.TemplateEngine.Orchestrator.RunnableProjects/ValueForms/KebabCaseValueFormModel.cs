using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ValueForms
{
    public class KebabCaseValueFormModel : IValueForm
    {
        public string Identifier => "kebabCase";

        public string Name { get; }

        public KebabCaseValueFormModel()
        {
        }

        public KebabCaseValueFormModel(string name)
        {
            Name = name;
        }

        public IValueForm FromJObject(string name, JObject configuration)
        {
            return new KebabCaseValueFormModel(name);
        }

        public string Process(IReadOnlyDictionary<string, IValueForm> forms, string value)
        {
            if (value is null)
            {
                return null;
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            Regex pattern = new Regex(@"(?:\p{Lu}\p{M}*)?(?:\p{Ll}\p{M}*)+|(?:\p{Lu}\p{M}*)+(?!\p{Ll})|\p{N}+|[^\p{C}\p{P}\p{Z}]+|[\u2700-\u27BF]");
            return string.Join("-", pattern.Matches(value).Cast<Match>().Select(m => m.Value)).ToLowerInvariant();
        }
    }
}
