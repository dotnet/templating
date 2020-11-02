using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.TemplateResolution;
using Microsoft.TemplateEngine.Edge.Template;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Cli.CommandParsing
{
    class TemplateFilterOption : FilterOption
    {
        internal Func<INewCommandInput, Func<ITemplateInfo, MatchInfo?>> TemplateMatchFilter { get; set; }
        internal Func<ListOrHelpTemplateListResolutionResult, bool> MismatchCriteria { get; set; }
    }
}
