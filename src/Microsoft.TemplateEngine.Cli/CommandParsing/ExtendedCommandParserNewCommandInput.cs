using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli.CommandParsing
{
    public class ExtendedCommandParserNewCommandInput : INewCommandInput
    {
        private readonly ExtendedCommandParser _parser;
        private readonly Action<ExtendedCommandParser> _resetter;
        private readonly CommandArgument _templateNameArg;

        public ExtendedCommandParserNewCommandInput(ExtendedCommandParser parser, Action<ExtendedCommandParser> resetter)
        {
            _parser = parser;
            _resetter = resetter;

            _templateNameArg = _parser.Argument("template", LocalizableStrings.TemplateArgumentHelp);
        }

        public string TemplateName => _templateNameArg.Value;

        public string Alias => _parser.InternalParamValue("--alias");

        public IList<string> ExtraArgs => _parser.InternalParamValueList("--extra-args");

        public IList<string> ToInstallList => _parser.InternalParamValueList("--install");

        public bool IsForceFlagSpecified => _parser.InternalParamHasValue("--force");

        public bool IsHelpFlagSpecified => _parser.InternalParamHasValue("--help");

        public bool IsListFlagSpecified => _parser.InternalParamHasValue("--list");

        public bool IsQuietFlagSpecified => _parser.InternalParamHasValue("--quiet");

        public bool IsShowAllFlagSpecified => _parser.InternalParamHasValue("--show-all");

        public string TypeFilter => _parser.InternalParamValue("--type");

        public string Language => _parser.InternalParamValue("--language");

        public string Locale => _parser.InternalParamValue("--locale");

        public string Name
        {
            get
            {
                string specifiedName = _parser.InternalParamValue("--name");

                if (string.IsNullOrWhiteSpace(specifiedName))
                {
                    return null;
                }

                return specifiedName;
            }
        }

        public string OutputPath => _parser.InternalParamValue("--output");

        public bool SkipUpdateCheck => _parser.InternalParamHasValue("--skip-update-check");

        public string AllowScriptsToRun => _parser.InternalParamValue("--allow-scripts");

        public IReadOnlyDictionary<string, string> AllTemplateParams => _parser.AllTemplateParams;

        public string TemplateParamInputFormat(string canonical)
        {
            return _parser.TemplateParamInputFormat(canonical);
        }

        public IReadOnlyList<string> VariantsForCanonical(string canonical)
        {
            return _parser.CanonicalToVariantsTemplateParamMap[canonical].ToList();
        }

        public bool TryGetCanonicalNameForVariant(string variant, out string canonical)
        {
            return _parser.TryGetCanonicalNameForVariant(variant, out canonical);
        }

        public bool HasDebuggingFlag(string flag)
        {
            return RemainingArguments.Any(x => x == flag);
        }

        public List<string> RemainingArguments => _parser.RemainingArguments;

        public IDictionary<string, IList<string>> RemainingParameters => _parser.RemainingParameters;

        public bool TemplateParamHasValue(string paramName)
        {
            return _parser.TemplateParamHasValue(paramName);
        }

        public string TemplateParamValue(string paramName)
        {
            return _parser.TemplateParamValue(paramName);
        }

        public void ParseArgs(IList<string> extraArgFileNames = null)
        {
            _parser.ParseArgs(extraArgFileNames);
        }

        public void ReParseForTemplate(ITemplateInfo templateInfo, HostSpecificTemplateData hostSpecificTemplateData)
        {
            _resetter(_parser);

            IReadOnlyList<ITemplateParameter> parameterDefinitions = templateInfo.Parameters;

            IEnumerable<KeyValuePair<string, string>> argParameters = parameterDefinitions
                                                            .Where(x => x.Priority != TemplateParameterPriority.Implicit)
                                                            .OrderBy(x => x.Name)
                                                            .Select(x => new KeyValuePair<string, string>(x.Name, x.DataType));

            _parser.SetupTemplateParameters(argParameters, hostSpecificTemplateData.LongNameOverrides, hostSpecificTemplateData.ShortNameOverrides);
            ParseArgs(ExtraArgs);
        }

        public string HelpText
        {
            get
            {
                return _parser.HelpView();
            }
        }

        public int Execute(params string[] args)
        {
            return _parser.Execute(args);
        }

        public void OnExecute(Func<Task<CreationResultStatus>> invoke)
        {
            _parser.OnExecute(invoke);
        }
    }
}
