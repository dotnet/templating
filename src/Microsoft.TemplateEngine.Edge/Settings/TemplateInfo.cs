using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    public class TemplateInfo : ITemplateInfo
    {
        public TemplateInfo()
        {
        }

        public static TemplateInfo FromJObject(JObject entry, string cacheVersion)
        {
            if (string.Equals(cacheVersion, "1.0.0.0"))
            {
                return FromCacheEntryForCacheVersion1_0_0_0(entry);
            }
            else
            {
                return FromCacheEntryForOriginalUnversionedCache(entry);
            }
        }

        private static TemplateInfo FromCacheEntryForOriginalUnversionedCache(JObject entry)
        {
            TemplateInfo info = new TemplateInfo();

            info.ConfigMountPointId = Guid.Parse(entry.ToString(nameof(ConfigMountPointId)));
            info.Author = entry.ToString(nameof(Author));
            JArray classificationsArray = entry.Get<JArray>(nameof(Classifications));

            List<string> classifications = new List<string>();
            info.Classifications = classifications;
            //using (Timing.Over("Read classifications"))
            foreach (JToken item in classificationsArray)
            {
                classifications.Add(item.ToString());
            }

            info.DefaultName = entry.ToString(nameof(DefaultName));
            info.Description = entry.ToString(nameof(Description));
            info.Identity = entry.ToString(nameof(Identity));
            info.GeneratorId = Guid.Parse(entry.ToString(nameof(GeneratorId)));
            info.GroupIdentity = entry.ToString(nameof(GroupIdentity));
            info.Name = entry.ToString(nameof(Name));
            info.ShortName = entry.ToString(nameof(ShortName));

            // tags are just "name": "description"
            // e.g.: "language": "C#"
            JObject tagsObject = entry.Get<JObject>(nameof(Tags));
            Dictionary<string, ICacheTag> tags = new Dictionary<string, ICacheTag>();
            info.Tags = tags;
            foreach (JProperty item in tagsObject.Properties())
            {
                Dictionary<string, string> choicesAndDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                choicesAndDescriptions.Add(item.Value.ToString(), string.Empty);
                ICacheTag cacheTag = new CacheTag(
                    string.Empty,       // description
                    choicesAndDescriptions,
                    item.Value.ToString());

                tags.Add(item.Name.ToString(), cacheTag);
            }

            info.ConfigPlace = entry.ToString(nameof(ConfigPlace));
            info.LocaleConfigMountPointId = Guid.Parse(entry.ToString(nameof(LocaleConfigMountPointId)));
            info.LocaleConfigPlace = entry.ToString(nameof(LocaleConfigPlace));

            return info;
        }

        private static TemplateInfo FromCacheEntryForCacheVersion1_0_0_0(JObject entry)
        {
            TemplateInfo info = new TemplateInfo();

            info.ConfigMountPointId = Guid.Parse(entry.ToString(nameof(ConfigMountPointId)));
            info.Author = entry.ToString(nameof(Author));
            JArray classificationsArray = entry.Get<JArray>(nameof(Classifications));

            List<string> classifications = new List<string>();
            info.Classifications = classifications;
            //using (Timing.Over("Read classifications"))
                foreach (JToken item in classificationsArray)
                {
                    classifications.Add(item.ToString());
                }

            info.DefaultName = entry.ToString(nameof(DefaultName));
            info.Description = entry.ToString(nameof(Description));
            info.Identity = entry.ToString(nameof(Identity));
            info.GeneratorId = Guid.Parse(entry.ToString(nameof(GeneratorId)));
            info.GroupIdentity = entry.ToString(nameof(GroupIdentity));
            info.Precedence = entry.ToInt32(nameof(Precedence));
            info.Name = entry.ToString(nameof(Name));
            info.ShortName = entry.ToString(nameof(ShortName));

            // parse the cached tags
            Dictionary<string, ICacheTag> tags = new Dictionary<string, ICacheTag>();
            JObject tagsObject = entry.Get<JObject>(nameof(Tags));
            foreach (JProperty item in tagsObject.Properties())
            {
                Dictionary<string, string> choicesAndDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                JObject cdToken = item.Value.Get<JObject>(nameof(ICacheTag.ChoicesAndDescriptions));
                foreach (JProperty cdPair in cdToken.Properties())
                {
                    choicesAndDescriptions.Add(cdPair.Name.ToString(), cdPair.Value.ToString());
                }

                ICacheTag cacheTag = new CacheTag(
                    item.Value.ToString(nameof(ICacheTag.Description)),
                    choicesAndDescriptions, 
                    item.Value.ToString(nameof(ICacheTag.DefaultValue)));
                tags.Add(item.Name.ToString(), cacheTag);
            }
            info.Tags = tags;

            // parse the cached params
            JObject cacheParamsObject = entry.Get<JObject>(nameof(CacheParameters));
            Dictionary<string, ICacheParameter> cacheParams = new Dictionary<string, ICacheParameter>();
            foreach (JProperty item in cacheParamsObject.Properties())
            {
                ICacheParameter param = new CacheParameter
                {
                    DataType = item.Value.ToString(nameof(ICacheParameter.DataType)),
                    DefaultValue = item.Value.ToString(nameof(ICacheParameter.DefaultValue)),
                    Description = item.Value.ToString(nameof(ICacheParameter.Description))
                };

                cacheParams[item.Name.ToString()] = param;
            }
            info.CacheParameters = cacheParams;

            info.ConfigPlace = entry.ToString(nameof(ConfigPlace));
            info.LocaleConfigMountPointId = Guid.Parse(entry.ToString(nameof(LocaleConfigMountPointId)));
            info.LocaleConfigPlace = entry.ToString(nameof(LocaleConfigPlace));

            info.HostConfigMountPointId = Guid.Parse(entry.ToString(nameof(HostConfigMountPointId)));
            info.HostConfigPlace = entry.ToString(nameof(HostConfigPlace));

            return info;
        }

        [JsonProperty]
        public Guid ConfigMountPointId { get; set; }

        [JsonProperty]
        public string Author { get; set; }

        [JsonProperty]
        public IReadOnlyList<string> Classifications { get; set; }

        [JsonProperty]
        public string DefaultName { get; set; }

        [JsonProperty]
        public string Description { get; set; }

        [JsonProperty]
        public string Identity { get; set; }

        [JsonProperty]
        public Guid GeneratorId { get; set; }

        [JsonProperty]
        public string GroupIdentity { get; set; }

        [JsonProperty]
        public int Precedence { get; set; }

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public string ShortName { get; set; }

        [JsonProperty]
        public IReadOnlyDictionary<string, ICacheTag> Tags { get; set; }

        [JsonProperty]
        public IReadOnlyDictionary<string, ICacheParameter> CacheParameters { get; set; }

        [JsonProperty]
        public string ConfigPlace { get; set; }

        [JsonProperty]
        public Guid LocaleConfigMountPointId { get; set; }

        [JsonProperty]
        public string LocaleConfigPlace { get; set; }

        [JsonProperty]
        public Guid HostConfigMountPointId { get; set; }

        [JsonProperty]
        public string HostConfigPlace { get; set; }
    }
}
