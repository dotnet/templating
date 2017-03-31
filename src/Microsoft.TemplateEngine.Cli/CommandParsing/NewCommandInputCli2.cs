using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Cli.CommandLine;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli.CommandParsing
{
    public class NewCommandInputCli2 : INewCommandInput
    {
        private ParseResult _parseResult;
        private string[] _args;
        private string _templateNameArg;

        private Command _currentCommand;
        private Func<Task<int>> _invoke;

        private IReadOnlyDictionary<string, string> _templateParamValues;
        private IDictionary<string, IList<string>> _templateParamCanonicalToVariantMap;

        // used for parsing args outside the context of a specific template.
        private readonly Command _noTemplateCommand;
        private readonly string _commandName;

        public NewCommandInputCli2(string commandName)
        {
            _commandName = commandName;
            _noTemplateCommand = CommandParserSupport.NewWithActiveArgs(_commandName);
            _currentCommand = _noTemplateCommand;
        }

        public int Execute(params string[] args)
        {
            _args = args;
            ParseArgs();

            if (_parseResult.TryGetAppliedOption(out IList<string> templateNameList, new[] { _commandName }))
            {
                if (templateNameList.Count > 0)
                {
                    _templateNameArg = templateNameList[0];
                }
                else
                {
                    _templateNameArg = string.Empty;
                }
            }
            else
            {
                _templateNameArg = string.Empty;
            }

            return _invoke.Invoke().Result;
        }

        public void OnExecute(Func<Task<CreationResultStatus>> invoke)
        {
            _invoke = async () => (int)await invoke().ConfigureAwait(false);
        }

        public void ParseArgs(IList<string> extraArgFileNames = null)
        {
            List<string> argsWithCommand = new List<string>() { _commandName };
            argsWithCommand.AddRange(_args.ToList());

            if (extraArgFileNames != null)
            {
                argsWithCommand.AddRange(AppExtensions.CreateArgListFromAdditionalFiles(extraArgFileNames));
            }

            Parser parser = new Parser(new[] { '=' }, _currentCommand);
            _parseResult = parser.Parse(argsWithCommand.ToArray());
            
            _templateParamCanonicalToVariantMap = null;
        }

        public void ReParseForTemplate(ITemplateInfo templateInfo, HostSpecificTemplateData hostSpecificTemplateData)
        {
            List<ITemplateParameter> filteredParams = templateInfo.Parameters.Where(x => !string.Equals(x.Name, "type") && !string.Equals(x.Name, "language")).ToList();
            Command _templateSpecificCommand;

            try
            {
                _templateSpecificCommand = CommandParserSupport.CreateNewCommandWithArgsForTemplate(
                            _commandName,
                            _templateNameArg,
                            filteredParams,
                            hostSpecificTemplateData.LongNameOverrides,
                            hostSpecificTemplateData.ShortNameOverrides,
                            out Dictionary<string, IList<string>> templateParamMap);

                _currentCommand = _templateSpecificCommand;
                ParseArgs();

                //Console.WriteLine($"Template = {templateInfo.ShortName} | {templateInfo.Identity}: {_parseResult.ToString()}");

                // this must happen after ParseArgs(), which resets _templateParamCanonicalToVariantMap
                _templateParamCanonicalToVariantMap = templateParamMap;

                Dictionary<string, string> templateParamValues = new Dictionary<string, string>();

                foreach (KeyValuePair<string, IList<string>> paramInfo in _templateParamCanonicalToVariantMap)
                {
                    string paramName = paramInfo.Key;
                    string firstVariant = paramInfo.Value[0];

                    if (_parseResult.TryGetAppliedOption(out string inputValue, new[] { _commandName, firstVariant }))
                    {
                        templateParamValues.Add(paramName, inputValue);
                        //Console.WriteLine($"\t{paramName} = {inputValue}");
                    }
                }

                _templateParamValues = templateParamValues;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up parser for template. Template name = {templateInfo.Name} | ShortName = {templateInfo.ShortName} | Precedence = {templateInfo.Precedence}");
                Console.WriteLine(ex.StackTrace);
                throw ex;
            }
        }

        public string TemplateName => _templateNameArg;

        public string Alias => _parseResult.GetAppliedOptionOrDefault<string>(new[] { _commandName, "alias" });

        public IList<string> ExtraArgs => _parseResult.GetArgumentsAtPath(new[] { _commandName, "extra-args" }).ToList();

        public IList<string> ToInstallList => _parseResult.GetArgumentsAtPath(new[] { _commandName, "install" }).ToList();

        public bool IsForceFlagSpecified => _parseResult.HasAppliedOption(new[] { _commandName, "force" });

        public bool IsHelpFlagSpecified => _parseResult.HasAppliedOption(new[] { _commandName, "help" });

        public bool IsListFlagSpecified => _parseResult.HasAppliedOption(new[] { _commandName, "list" });

        public bool IsQuietFlagSpecified => _parseResult.HasAppliedOption(new[] { _commandName, "quiet" });

        public bool IsShowAllFlagSpecified => _parseResult.HasAppliedOption(new[] { _commandName, "all" });

        public string TypeFilter => _parseResult.GetAppliedOptionOrDefault<string>(new[] { _commandName, "type" });

        public string Language => _parseResult.GetAppliedOptionOrDefault<string>(new[] { _commandName, "language" });

        public string Locale => _parseResult.GetAppliedOptionOrDefault<string>(new[] { _commandName, "locale" });

        public string Name => _parseResult.GetAppliedOptionOrDefault<string>(new[] { _commandName, "name" });

        public string OutputPath => _parseResult.GetAppliedOptionOrDefault<string>(new[] { _commandName, "output" });

        public bool SkipUpdateCheck => _parseResult.HasAppliedOption(new[] { _commandName, "skip-update-check" });

        public string AllowScriptsToRun => _parseResult.GetAppliedOptionOrDefault<string>(new[] { _commandName, "allow-scripts" });

        public bool HasDebuggingFlag(string flag)
        {
            return _parseResult.HasAppliedOption(new[] { _commandName, flag });
        }

        public IReadOnlyDictionary<string, string> AllTemplateParams
        {
            get
            {
                if (_templateParamValues == null)
                {
                    _templateParamValues = new Dictionary<string, string>();
                }

                return _templateParamValues;
            }
        }

        public string TemplateParamInputFormat(string canonical)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<string> VariantsForCanonical(string canonical)
        {
            if (_templateParamCanonicalToVariantMap == null || !_templateParamCanonicalToVariantMap.TryGetValue(canonical, out IList<string> variants))
            {
                return new List<string>();
            }

            return variants.ToList();
        }

        public bool TryGetCanonicalNameForVariant(string variant, out string canonical)
        {
            return TemplateParamVariantToCanonicalMap.TryGetValue(variant, out canonical);
        }

        // Maps the template-related flag variants to their canonical
        private IReadOnlyDictionary<string, string> TemplateParamVariantToCanonicalMap
        {
            get
            {
                if (_templateParamVariantToCanonicalMap == null)
                {
                    Dictionary<string, string> map = new Dictionary<string, string>();

                    if (_templateParamCanonicalToVariantMap != null)
                    {
                        foreach (KeyValuePair<string, IList<string>> canonicalToVariants in _templateParamCanonicalToVariantMap)
                        {
                            string canonical = canonicalToVariants.Key;

                            foreach (string variant in canonicalToVariants.Value)
                            {
                                map.Add(variant, canonical);
                            }
                        }
                    }

                    _templateParamVariantToCanonicalMap = map;
                }

                return _templateParamVariantToCanonicalMap;
            }

        }
        private IReadOnlyDictionary<string, string> _templateParamVariantToCanonicalMap;


        // TODO: verify this
        public List<string> RemainingArguments
        {
            get
            {
                return _parseResult.UnmatchedTokens.ToList();
            }
        }

        // TODO: fill this in correctly
        public IDictionary<string, IList<string>> RemainingParameters
        {
            get
            {
                return new Dictionary<string, IList<string>>();
            }
        }

        public bool TemplateParamHasValue(string paramName)
        {
            return AllTemplateParams.ContainsKey(paramName);
        }

        public string TemplateParamValue(string paramName)
        {
            AllTemplateParams.TryGetValue(paramName, out string value);
            return value;
        }

        public string HelpText
        {
            get
            {
                return _noTemplateCommand.HelpView();
            }
        }
    }
}
