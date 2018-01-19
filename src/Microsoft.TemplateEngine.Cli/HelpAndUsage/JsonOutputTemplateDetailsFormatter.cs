using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Cli.HelpAndUsage
{
    public class JsonOutputTemplateDetailsFormatter
    {
        public JsonOutputTemplateDetailsFormatter(IReadOnlyList<ITemplateInfo> templateGroup, TemplateGroupParameterDetails groupParameterDetails, bool showImplicitlyHiddenParams)
        {
            _templateGroup = templateGroup;
            _groupParameterDetails = groupParameterDetails;
            _showImplicitlyHiddenParams = showImplicitlyHiddenParams;
        }

        private readonly IReadOnlyList<ITemplateInfo> _templateGroup;
        private readonly TemplateGroupParameterDetails _groupParameterDetails;
        private readonly bool _showImplicitlyHiddenParams;

        private JsonOutputTemplateDetails _templateDetails;
        private string _jsonSerializedDetails;

        public JsonOutputTemplateDetails TemplateDetails
        {
            get
            {
                EnsureTemplateDetails();

                return _templateDetails;
            }
        }

        public string JsonSerializedDetails
        {
            get
            {
                EnsureTemplateDetails();

                if (_jsonSerializedDetails == null)
                {
                    _jsonSerializedDetails = JObject.FromObject(_templateDetails).ToString();
                }

                return _jsonSerializedDetails;
            }
        }

        private void EnsureTemplateDetails()
        {
            if (_templateDetails != null)
            {
                return;
            }

            // use all templates to get the language choices
            HashSet<string> languages = new HashSet<string>();
            foreach (ITemplateInfo templateInfo in _templateGroup)
            {
                if (templateInfo.Tags != null && templateInfo.Tags.TryGetValue("language", out ICacheTag languageTag))
                {
                    languages.UnionWith(languageTag.ChoicesAndDescriptions.Keys.Where(x => !string.IsNullOrWhiteSpace(x)).ToList());
                }
            }

            // Use the highest precedence template for most of the output
            ITemplateInfo preferredTemplate = _templateGroup.OrderByDescending(x => x.Precedence).First();

            string type = null;
            if (preferredTemplate.Tags != null && preferredTemplate.Tags.TryGetValue("type", out ICacheTag typeTag))
            {
                type = typeTag.ChoicesAndDescriptions.Keys.FirstOrDefault();
            }

            _templateDetails = new JsonOutputTemplateDetails()
            {
                Author = preferredTemplate.Author,
                Classifications = preferredTemplate.Classifications,
                Description = preferredTemplate.Description,
                Identity = preferredTemplate.Identity,
                GroupIdentity = preferredTemplate.GroupIdentity,
                Name = preferredTemplate.Name,
                ShortName = preferredTemplate.ShortName,
                Parameters = SetupParameters(),
                Languages = languages.ToList(),
                Type = type
            };
        }

        private IReadOnlyList<JsonOutputTemplateParameter> SetupParameters()
        {
            // TODO: decide if we need to use _groupParameterDetails.AdditionalInfo
            //  it's a minor error output for console display mode.

            IEnumerable<ITemplateParameter> filteredParams = TemplateParameterHelpBase.FilterParamsForHelp(_groupParameterDetails.AllParams.ParameterDefinitions, _groupParameterDetails.ExplicitlyHiddenParams,
                                                                        _showImplicitlyHiddenParams, _groupParameterDetails.HasPostActionScriptRunner, _groupParameterDetails.ParametersToAlwaysShow);

            List<JsonOutputTemplateParameter> jsonParameterList = new List<JsonOutputTemplateParameter>();

            // TODO: deal with "allow-scripts" ???
            foreach (ITemplateParameter templateParameter in filteredParams)
            {
                JsonOutputTemplateParameter jsonParameter = new JsonOutputTemplateParameter()
                {
                    Name = templateParameter.Name,
                    CliOptionVariants = _groupParameterDetails.GroupVariantsForCanonicals[templateParameter.Name],
                    DataType = templateParameter.DataType,
                    Description = templateParameter.Documentation,
                    DefaultValue = templateParameter.DefaultValue,
                    ChoicesAndDescriptions = templateParameter.Choices
                };

                jsonParameterList.Add(jsonParameter);
            }

            return jsonParameterList;
        }
    }
}
