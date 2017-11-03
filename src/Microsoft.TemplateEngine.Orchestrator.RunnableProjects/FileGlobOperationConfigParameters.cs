using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    internal enum ConditionalOperationConfigType
    {
        None,
        CBlockComment,
        CLineComment,
        CNoComment,
        HamlLineComment,
        HashLineComment,
        JsxBlockComment,
        MSBuildInline,
        RazorBlockComment,
        RemLineComment,
        XmlBlockComment
    };

    internal class FileGlobOperationConfigParameters
    {
        public FileGlobOperationConfigParameters(string glob, string flagPrefix, params ConditionalOperationConfigType[] conditionalConfigs)
        {
            Glob = glob;
            FlagPrefix = flagPrefix;
            ConditionalConfigs = conditionalConfigs;
        }

        public string Glob { get; }

        public string FlagPrefix { get; }

        public IReadOnlyList<ConditionalOperationConfigType> ConditionalConfigs { get; }

        private static readonly FileGlobOperationConfigParameters _Defaults = new FileGlobOperationConfigParameters(string.Empty, string.Empty, ConditionalOperationConfigType.None);

        public static FileGlobOperationConfigParameters Defaults => _Defaults;
    }
}
