using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli.HelpAndUsage
{
    public struct TemplateGroupParameterDetails
    {
        public IParameterSet AllParams;
        public string AdditionalInfo;       // TODO: rename (probably)
        public IReadOnlyList<string> InvalidParams;
        public HashSet<string> ExplicitlyHiddenParams;
        public IReadOnlyDictionary<string, IReadOnlyList<string>> GroupVariantsForCanonicals;
        public HashSet<string> GroupUserParamsWithDefaultValues;
        public bool HasPostActionScriptRunner;
        public HashSet<string> ParametersToAlwaysShow;
    }
}
