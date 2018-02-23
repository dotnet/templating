using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Utils
{
    public class TemplateParameter : ITemplateParameter, IAllowDefaultIfOptionWithoutValue
    {
        public string Documentation { get; set; }

        public string Name { get; set; }

        public TemplateParameterPriority Priority { get; set; }

        public string Type { get; set; }

        public bool IsName { get; set; }

        public string DefaultValue { get; set; }

        public string DataType { get; set; }

        public string DefaultIfOptionWithoutValue { get; set; }

        private IReadOnlyDictionary<string, string> _choices;

        public IReadOnlyDictionary<string, string> Choices
        {
            get
            {
                return _choices;
            }
            set
            {
                _choices = value.CloneIfDifferentComparer(StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
