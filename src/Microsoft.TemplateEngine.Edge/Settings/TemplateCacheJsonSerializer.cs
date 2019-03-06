using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    internal class TemplateCacheJsonSerializer
    {
        public TemplateCacheJsonSerializer()
        {
        }

        public bool TrySerialize(TemplateCache cache, out string serialized)
        {
            JObject serializedCache = new JObject();

            List<JObject> templateObjects = new List<JObject>();
            foreach (TemplateInfo template in cache.TemplateInfo)
            {
                if (TrySerializeTemplate(template, out JObject serializedTemplate))
                {
                    templateObjects.Add(serializedTemplate);
                }
                else
                {
                    serialized = null;
                    return false;
                }
            }

            serializedCache.Add(nameof(TemplateCache.TemplateInfo), JArray.FromObject(templateObjects));

            serialized = serializedCache.ToString();
            return true;
        }

        private bool TrySerializeTemplate(TemplateInfo template, out JObject serializedTemplate)
        {
            try
            {
                serializedTemplate = new JObject();
                serializedTemplate.Add(nameof(TemplateInfo.ConfigMountPointId), template.ConfigMountPointId);
                serializedTemplate.Add(nameof(TemplateInfo.Author), template.Author);
                serializedTemplate.Add(nameof(TemplateInfo.Classifications), JArray.FromObject(template.Classifications));
                serializedTemplate.Add(nameof(TemplateInfo.DefaultName), template.DefaultName);
                serializedTemplate.Add(nameof(TemplateInfo.Description), template.Description);
                serializedTemplate.Add(nameof(TemplateInfo.Identity), template.Identity);
                serializedTemplate.Add(nameof(TemplateInfo.GeneratorId), template.GeneratorId);
                serializedTemplate.Add(nameof(TemplateInfo.GroupIdentity), template.GroupIdentity);
                serializedTemplate.Add(nameof(TemplateInfo.Precedence), template.Precedence);
                serializedTemplate.Add(nameof(TemplateInfo.Name), template.Name);
                serializedTemplate.Add(nameof(TemplateInfo.ShortNameList), JArray.FromObject(template.ShortNameList));

                if (TrySerializeCacheTagDictionary(template.Tags, out JObject tagListObject))
                {
                    serializedTemplate.Add(nameof(TemplateInfo.Tags), tagListObject);
                }

                if (TrySerializeCacheParameterDictionary(template.CacheParameters, out JObject cacheParamObject))
                {
                    serializedTemplate.Add(nameof(TemplateInfo.CacheParameters), cacheParamObject);
                }

                // Note: Parameters are not serialized. Everything needed from them is in Tags & CacheParameters

                serializedTemplate.Add(nameof(TemplateInfo.ConfigPlace), template.ConfigPlace);
                serializedTemplate.Add(nameof(TemplateInfo.LocaleConfigMountPointId), template.LocaleConfigMountPointId);
                serializedTemplate.Add(nameof(TemplateInfo.LocaleConfigPlace), template.LocaleConfigPlace);
                serializedTemplate.Add(nameof(TemplateInfo.HostConfigMountPointId), template.HostConfigMountPointId);
                serializedTemplate.Add(nameof(TemplateInfo.HostConfigPlace), template.HostConfigPlace);
                serializedTemplate.Add(nameof(TemplateInfo.ThirdPartyNotices), template.ThirdPartyNotices);

                if (TrySerializeBaselinesDictionary(template.BaselineInfo, out JObject baselineInfoObject))
                {
                    serializedTemplate.Add(nameof(TemplateInfo.BaselineInfo), baselineInfoObject);
                }

                serializedTemplate.Add(nameof(TemplateInfo.HasScriptRunningPostActions), template.HasScriptRunningPostActions);
                serializedTemplate.Add(nameof(TemplateInfo.ConfigTimestampUtc), template.ConfigTimestampUtc);

                return true;
            }
            catch
            {
                serializedTemplate = null;
                return false;
            }
        }

        private bool TrySerializeCacheTagDictionary(IReadOnlyDictionary<string, ICacheTag> tagDict, out JObject tagDictObject)
        {
            try
            {
                tagDictObject = new JObject();

                foreach (KeyValuePair<string, ICacheTag> cacheTagInfo in tagDict)
                {
                    if (TrySerializeCacheTag(cacheTagInfo.Value, out JObject serializedCacheTag))
                    {
                        tagDictObject.Add(cacheTagInfo.Key, serializedCacheTag);
                    }
                }

                return true;
            }
            catch
            {
                tagDictObject = null;
                return false;
            }
        }

        // TODO: after IAllowDefaultIfOptionWithoutValue is rolled up into ICacheParameter, get rid of the extra check for it.
        private bool TrySerializeCacheTag(ICacheTag tag, out JObject tagObject)
        {
            try
            {
                tagObject = new JObject();
                tagObject.Add(nameof(ICacheTag.Description), tag.Description);

                if (TrySerializeStringDictionary(tag.ChoicesAndDescriptions, out JObject serializedChoicesAndDescriptions))
                {
                    tagObject.Add(nameof(ICacheTag.ChoicesAndDescriptions), serializedChoicesAndDescriptions);
                }

                tagObject.Add(nameof(ICacheTag.DefaultValue), tag.DefaultValue);

                if (tag is IAllowDefaultIfOptionWithoutValue tagWithNoValueDefault
                        && !string.IsNullOrEmpty(tagWithNoValueDefault.DefaultIfOptionWithoutValue))
                {
                    tagObject.Add(nameof(IAllowDefaultIfOptionWithoutValue.DefaultIfOptionWithoutValue), tagWithNoValueDefault.DefaultIfOptionWithoutValue);
                }


                return true;
            }
            catch
            {
                tagObject = null;
                return false;
            }
        }

        private bool TrySerializeCacheParameterDictionary(IReadOnlyDictionary<string, ICacheParameter> parameterDict, out JObject paramDictObject)
        {
            try
            {
                paramDictObject = new JObject();

                foreach (KeyValuePair<string, ICacheParameter> paramInfo in parameterDict)
                {
                    if (TrySerializeCacheParameter(paramInfo.Value, out JObject serializedCacheParam))
                    {
                        paramDictObject.Add(paramInfo.Key, serializedCacheParam);
                    }
                }

                return true;
            }
            catch
            {
                paramDictObject = null;
                return false;
            }
        }

        // TODO: after IAllowDefaultIfOptionWithoutValue is rolled up into ICacheParameter, get rid of the extra check for it.
        private bool TrySerializeCacheParameter(ICacheParameter param, out JObject paramObject)
        {
            try
            {
                paramObject = new JObject();
                paramObject.Add(nameof(ICacheParameter.DataType), param.DataType);
                paramObject.Add(nameof(ICacheParameter.DefaultValue), param.DefaultValue);
                paramObject.Add(nameof(ICacheParameter.Description), param.Description);

                if (param is IAllowDefaultIfOptionWithoutValue paramWithNoValueDefault
                    && !string.IsNullOrEmpty(paramWithNoValueDefault.DefaultIfOptionWithoutValue))
                {
                    paramObject.Add(nameof(IAllowDefaultIfOptionWithoutValue.DefaultIfOptionWithoutValue), paramWithNoValueDefault.DefaultIfOptionWithoutValue);
                }

                return true;
            }
            catch
            {
                paramObject = null;
                return false;
            }
        }

        private bool TrySerializeBaselinesDictionary(IReadOnlyDictionary<string, IBaselineInfo> baselineDict, out JObject baselineDictObject)
        {
            try
            {
                baselineDictObject = new JObject();

                foreach (KeyValuePair<string, IBaselineInfo> baseline in baselineDict)
                {
                    if (TrySerializeBaseline(baseline.Value, out JObject baselineObject))
                    {
                        baselineDictObject.Add(baseline.Key, baselineObject);
                    }
                }

                return true;
            }
            catch
            {
                baselineDictObject = null;
                return false;
            }
        }

        private bool TrySerializeBaseline(IBaselineInfo baseline, out JObject baselineObject)
        {
            try
            {
                baselineObject = new JObject();
                baselineObject.Add(nameof(IBaselineInfo.Description), baseline.Description);
                if (TrySerializeStringDictionary(baseline.DefaultOverrides, out JObject defaultsObject))
                {
                    baselineObject.Add(nameof(IBaselineInfo.DefaultOverrides), defaultsObject);
                }

                return true;
            }
            catch
            {
                baselineObject = null;
                return false;
            }
        }

        private bool TrySerializeStringDictionary(IReadOnlyDictionary<string, string> toSerialize, out JObject serialized)
        {
            try
            {
                serialized = new JObject();

                foreach (KeyValuePair<string, string> entry in toSerialize)
                {
                    serialized.Add(entry.Key, entry.Value);
                }

                return true;
            }
            catch
            {
                serialized = null;
                return false;
            }
        }
    }
}
