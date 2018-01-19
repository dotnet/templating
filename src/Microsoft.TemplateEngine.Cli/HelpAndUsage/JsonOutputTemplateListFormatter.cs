using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge.Template;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Cli.HelpAndUsage
{
    public class JsonOutputTemplateListFormatter
    {
        public JsonOutputTemplateListFormatter(IReadOnlyList<ITemplateMatchInfo> templateList)
        {
            _templateList = templateList;
        }

        private readonly IReadOnlyList<ITemplateMatchInfo> _templateList;
        private JsonOutputTemplateList _jsonTemplateInfoList;
        private string _jsonSerializedList;

        public JsonOutputTemplateList JsonTemplateInfoList
        {
            get
            {
                EnsureFormattedTemplateList();

                return _jsonTemplateInfoList;
            }
        }

        public string JsonSerializedList
        {
            get
            {
                EnsureFormattedTemplateList();

                if (_jsonSerializedList == null)
                {
                    _jsonSerializedList = JObject.FromObject(_jsonTemplateInfoList).ToString();
                }

                return _jsonSerializedList;
            }
        }

        private void EnsureFormattedTemplateList()
        {
            if (_jsonTemplateInfoList != null)
            {
                return;
            }

            List<JsonOutputTemplateInfo> jsonTemplateList = new List<JsonOutputTemplateInfo>();

            foreach (ITemplateMatchInfo template in _templateList)
            {
                // Note: This assumes there is at most 1 language per template.
                string language = null;
                if (template.Info.Tags != null && template.Info.Tags.TryGetValue("language", out ICacheTag languageTag))
                {
                    language = languageTag.ChoicesAndDescriptions.Keys.FirstOrDefault();
                }

                JsonOutputTemplateInfo jsonInfo = new JsonOutputTemplateInfo()
                {
                    GroupIndentity = template.Info.GroupIdentity,
                    Identity = template.Info.Identity,
                    Name = template.Info.Name,
                    ShortName = template.Info.ShortName,
                    Language = language,
                    Classifications = template.Info.Classifications
                };

                jsonTemplateList.Add(jsonInfo);
            }

            _jsonTemplateInfoList = new JsonOutputTemplateList(jsonTemplateList);
        }
    }
}
