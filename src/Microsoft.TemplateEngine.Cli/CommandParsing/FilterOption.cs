using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Cli.CommandParsing
{
    internal class FilterOption
    {
        internal string Name { get; set; }
        internal Func<INewCommandInput, string> FilterValue { get; set; }
        internal Func<INewCommandInput, bool> IsFilterSet { get; set; }
    }
}
