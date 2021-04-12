using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.TemplateEngine.Utils;

#nullable enable

namespace Microsoft.TemplateEngine.Edge.Settings
{
    public partial class TemplateInfo : ITemplateInfo, IShortNameList
    {
        public TemplateInfo(JObject jObject)
        {
            MountPointUri = jObject.ToString(nameof(MountPointUri)) ?? throw new ArgumentNullException(nameof(MountPointUri));
            Author = jObject.ToString(nameof(Author));
            DefaultName = jObject.ToString(nameof(DefaultName));
            Description = jObject.ToString(nameof(Description));
            Identity = jObject.ToString(nameof(Identity)) ?? throw new ArgumentNullException(nameof(Identity));
            GeneratorId = Guid.Parse(jObject.ToString(nameof(GeneratorId)) ?? throw new ArgumentNullException(nameof(GeneratorId)));
            GroupIdentity = jObject.ToString(nameof(GroupIdentity));
            Precedence = jObject.ToInt32(nameof(Precedence));
            Name = jObject.ToString(nameof(Name)) ?? throw new ArgumentNullException(nameof(Name));
            ConfigPlace = jObject.ToString(nameof(ConfigPlace)) ?? throw new ArgumentNullException(nameof(ConfigPlace));
            LocaleConfigPlace = jObject.ToString(nameof(LocaleConfigPlace));
            HostConfigPlace = jObject.ToString(nameof(HostConfigPlace));
            ThirdPartyNotices = jObject.ToString(nameof(ThirdPartyNotices));
            HasScriptRunningPostActions = jObject.ToBool(nameof(HasScriptRunningPostActions));

            ShortNameList = JTokenStringOrArrayToCollection(jObject.Get<JToken>(nameof(ShortNameList)), new string[0]);
            if (ShortNameList.Count == 0)
            {
                throw new Exception($"Cache is missing {ShortNameList}");
            }

            var classificationsArray = jObject.Get<JArray>(nameof(Classifications));
            List<string> classifications = new List<string>();
            Classifications = classifications;
            if (classificationsArray != null)
            {
                foreach (JToken item in classificationsArray)
                {
                    classifications.Add(item.ToString());
                }
            }

            JObject? tagsObject = jObject.Get<JObject>(nameof(Tags));
            Dictionary<string, ICacheTag> tags = new Dictionary<string, ICacheTag>();
            Tags = tags;
            if (tagsObject != null)
            {
                foreach (JProperty item in tagsObject.Properties())
                {
                    Dictionary<string, ParameterChoice> choicesAndDescriptions = new Dictionary<string, ParameterChoice>(StringComparer.OrdinalIgnoreCase);
                    choicesAndDescriptions.Add(item.Value.ToString(), new ParameterChoice(string.Empty, string.Empty));
                    ICacheTag cacheTag = new CacheTag(
                        displayName: string.Empty,
                        description: string.Empty,
                        choicesAndDescriptions,
                        item.Value.ToString());

                    tags.Add(item.Name, cacheTag);
                }
            }

            JObject? baselineJObject = jObject.Get<JObject>(nameof(ITemplateInfo.BaselineInfo));
            Dictionary<string, IBaselineInfo> baselineInfo = new Dictionary<string, IBaselineInfo>();
            if (baselineJObject != null)
            {
                foreach (JProperty item in baselineJObject.Properties())
                {
                    IBaselineInfo baseline = new BaselineCacheInfo()
                    {
                        Description = item.Value.ToString(nameof(IBaselineInfo.Description)),
                        DefaultOverrides = item.Value.ToStringDictionary(propertyName: nameof(IBaselineInfo.DefaultOverrides))
                    };
                    baselineInfo.Add(item.Name, baseline);
                }
            }
            BaselineInfo = baselineInfo;

            Tags = ReadTags(jObject);
            CacheParameters = ReadParameters(jObject);
        }

        private static IReadOnlyList<string> JTokenStringOrArrayToCollection(JToken? token, string[] defaultSet)
        {
            if (token == null)
            {
                return defaultSet;
            }

            if (token.Type == JTokenType.String)
            {
                string tokenValue = token.ToString();
                return new List<string>() { tokenValue };
            }

            return token.ArrayAsStrings();
        }

        private IReadOnlyDictionary<string, ICacheParameter> ReadParameters(JObject jObject)
        {
            Dictionary<string, ICacheParameter> cacheParams = new Dictionary<string, ICacheParameter>();
            JObject? cacheParamsObject = jObject.Get<JObject>(nameof(CacheParameters));

            if (cacheParamsObject != null)
            {
                foreach (JProperty item in cacheParamsObject.Properties())
                {
                    cacheParams[item.Name] = ReadOneParameter(item);
                }
            }

            return cacheParams;
        }

        private ICacheParameter ReadOneParameter(JProperty item)
        {
            return new CacheParameter
            {
                DataType = item.Value.ToString(nameof(CacheParameter.DataType)),
                DefaultValue = item.Value.ToString(nameof(CacheParameter.DefaultValue)),
                DisplayName = item.Value.ToString(nameof(CacheParameter.DisplayName)),
                Description = item.Value.ToString(nameof(CacheParameter.Description)),
                DefaultIfOptionWithoutValue = item.Value.ToString(nameof(CacheParameter.DefaultIfOptionWithoutValue))
            };
        }

        private IReadOnlyDictionary<string, ICacheTag> ReadTags(JObject jObject)
        {

            var tags = new Dictionary<string, ICacheTag>();
            var tagsObject = jObject.Get<JObject>(nameof(TemplateInfo.Tags));

            if (tagsObject != null)
            {
                foreach (JProperty item in tagsObject.Properties())
                {
                    tags[item.Name] = ReadOneTag(item);
                }
            }

            return tags;
        }

        private ICacheTag ReadOneTag(JProperty item)
        {
            var choices = new Dictionary<string, ParameterChoice>(StringComparer.OrdinalIgnoreCase);

            foreach (JProperty choiceObject in item.Value.PropertiesOf("Choices"))
            {
                choices.Add(choiceObject.Name, new ParameterChoice(
                    choiceObject.Value.ToString(nameof(ParameterChoice.DisplayName)),
                    choiceObject.Value.ToString(nameof(ParameterChoice.Description))));
            }

            return new CacheTag(
                displayName: item.Value.ToString(nameof(CacheTag.DisplayName)),
                description: item.Value.ToString(nameof(CacheTag.Description)),
                choices,
                item.Value.ToString(nameof(CacheTag.DefaultValue)),
                item.Value.ToString(nameof(CacheTag.DefaultIfOptionWithoutValue)));
        }

        [JsonIgnore]
        public IReadOnlyList<ITemplateParameter> Parameters
        {
            get
            {
                List<ITemplateParameter> parameters = new List<ITemplateParameter>();

                foreach (KeyValuePair<string, ICacheTag> tagInfo in Tags)
                {
                    TemplateParameter param = new TemplateParameter
                    {
                        Name = tagInfo.Key,
                        Documentation = tagInfo.Value.Description,
                        DefaultValue = tagInfo.Value.DefaultValue,
                        Choices = tagInfo.Value.Choices,
                        DataType = "choice"
                    };

                    if (tagInfo.Value is IAllowDefaultIfOptionWithoutValue tagWithNoValueDefault)
                    {
                        param.DefaultIfOptionWithoutValue = tagWithNoValueDefault.DefaultIfOptionWithoutValue;
                    }
                    parameters.Add(param);
                }

                foreach (KeyValuePair<string, ICacheParameter> paramInfo in CacheParameters)
                {
                    TemplateParameter param = new TemplateParameter
                    {
                        Name = paramInfo.Key,
                        Documentation = paramInfo.Value.Description,
                        DataType = paramInfo.Value.DataType,
                        DefaultValue = paramInfo.Value.DefaultValue,
                    };

                    if (paramInfo.Value is IAllowDefaultIfOptionWithoutValue infoWithNoValueDefault)
                    {
                        param.DefaultIfOptionWithoutValue = infoWithNoValueDefault.DefaultIfOptionWithoutValue;
                    }
                    parameters.Add(param);
                }
                return parameters;
            }
        }

        [JsonProperty]
        public string MountPointUri { get; }

        [JsonProperty]
        public string? Author { get; }

        [JsonProperty]
        public IReadOnlyList<string> Classifications { get; }

        [JsonProperty]
        public string? DefaultName { get; }

        [JsonProperty]
        public string? Description { get; }

        [JsonProperty]
        public string Identity { get; }

        [JsonProperty]
        public Guid GeneratorId { get; }

        [JsonProperty]
        public string? GroupIdentity { get; }

        [JsonProperty]
        public int Precedence { get; }

        [JsonProperty]
        public string Name { get; }

        [JsonIgnore]
        public string ShortName
        {
            get
            {
                if (ShortNameList.Count > 0)
                {
                    return ShortNameList[0];
                }

                return string.Empty;
            }
        }

        public IReadOnlyList<string> ShortNameList { get; }

        [JsonProperty]
        public IReadOnlyDictionary<string, ICacheTag> Tags { get; }

        [JsonProperty]
        public IReadOnlyDictionary<string, ICacheParameter> CacheParameters { get; }

        [JsonProperty]
        public string ConfigPlace { get; }

        [JsonProperty]
        public string? LocaleConfigPlace { get; }

        [JsonProperty]
        public string? HostConfigPlace { get; }

        [JsonProperty]
        public string? ThirdPartyNotices { get; }

        [JsonProperty]
        public IReadOnlyDictionary<string, IBaselineInfo> BaselineInfo { get; }

        [JsonProperty]
        public bool HasScriptRunningPostActions { get; }
    }
}
