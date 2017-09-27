using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.CommandParsing;
using Microsoft.TemplateEngine.Edge.Template;

namespace Microsoft.TemplateEngine.Cli.UnitTests.CliMocks
{
    internal class MockNewCommandInput : INewCommandInput
    {
        public MockNewCommandInput()
            : this(new Dictionary<string, string>())
        {
        }

        public MockNewCommandInput(IReadOnlyDictionary<string, string> rawParameterInputs)
        {
            _rawParameterInputs = rawParameterInputs;

            InputTemplateParams = new Dictionary<string, string>();
            RemainingParameters = new Dictionary<string, IList<string>>();
            RemainingArguments = new List<string>();
            _allParametersForTemplate = new List<string>();
        }

        // a list of all the parameters defined by the template
        private IReadOnlyList<string> _allParametersForTemplate;

        private IReadOnlyDictionary<string, string> _rawParameterInputs;

        public string CommandName => "MockNew";

        public string TemplateName { get; set; }

        public IReadOnlyList<string> Tokens { get; set; }

        public string Alias { get; set; }

        public bool ShowAliasesSpecified { get; set; }

        public string ShowAliasesAliasName { get; set; }

        public string BaselineName { get; set; }

        public IList<string> ExtraArgsFileNames { get; set; }

        public IList<string> ToInstallList { get; set; }

        public IList<string> ToUninstallList { get; set; }

        public bool IsForceFlagSpecified { get; set; }

        public bool IsHelpFlagSpecified { get; set; }

        public bool IsListFlagSpecified { get; set; }

        public bool IsQuietFlagSpecified { get; set; }

        public bool IsShowAllFlagSpecified { get; set; }

        public string TypeFilter { get; set; }

        public string Language { get; set; }

        public string Locale { get; set; }

        public string Name { get; set; }

        public string OutputPath { get; set; }

        public bool SkipUpdateCheck { get; set; }

        public string AllowScriptsToRun { get; set; }

        public IReadOnlyDictionary<string, string> InputTemplateParams { get; set; }

        public List<string> RemainingArguments { get; set; }

        public IDictionary<string, IList<string>> RemainingParameters { get; set; }

        public string HelpText { get; set; }

        public bool HasParseError { get; set; }

        public bool ExpandedExtraArgsFiles { get; set; }

        public int Execute(params string[] args)
        {
            throw new NotImplementedException();
        }

        public bool HasDebuggingFlag(string flag)
        {
            throw new NotImplementedException();
        }

        public void OnExecute(Func<Task<CreationResultStatus>> invoke)
        {
            throw new NotImplementedException();
        }

        public void ReparseForTemplate(ITemplateInfo templateInfo, HostSpecificTemplateData hostSpecificTemplateData)
        {
            Dictionary<string, string> templateParamValues = new Dictionary<string, string>();
            Dictionary<string, IList<string>> remainingParams = new Dictionary<string, IList<string>>();

            foreach (KeyValuePair<string, string> inputParam in _rawParameterInputs)
            {
                ITemplateParameter matchedParam = default(ITemplateParameter);

                if (templateInfo.Parameters != null)
                {
                    matchedParam = templateInfo.Parameters?.FirstOrDefault(x => string.Equals(x.Name, inputParam.Key));
                }

                if (matchedParam != default(ITemplateParameter))
                {
                    templateParamValues.Add(inputParam.Key, inputParam.Value);
                }
                else
                {
                    remainingParams.Add(inputParam.Key, new List<string>());
                }
            }

            InputTemplateParams = templateParamValues;
            RemainingParameters = remainingParams;
            RemainingArguments = remainingParams.Keys.ToList();

            _allParametersForTemplate = templateInfo.Parameters.Select(x => x.Name).ToList();
        }

        public void ResetArgs(params string[] args)
        {
            throw new NotImplementedException();
        }

        public bool TemplateParamHasValue(string paramName)
        {
            throw new NotImplementedException();
        }

        public string TemplateParamInputFormat(string canonical)
        {
            throw new NotImplementedException();
        }

        public string TemplateParamValue(string paramName)
        {
            throw new NotImplementedException();
        }

        // Note: This doesn't really deal with variants.
        // If the input "variant" is a parameter for the template, return true with the canonical set to the variant.
        // Otherwise return false with the canonical as null.
        public bool TryGetCanonicalNameForVariant(string variant, out string canonical)
        {
            if (_allParametersForTemplate.Contains(variant))
            {
                canonical = variant;
                return true;
            }

            canonical = null;
            return false;
        }

        public IReadOnlyList<string> VariantsForCanonical(string canonical)
        {
            throw new NotImplementedException();
        }
    }
}
