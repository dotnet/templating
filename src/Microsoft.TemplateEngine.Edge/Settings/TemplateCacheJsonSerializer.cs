using System;
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

        private static bool TrySerializeTemplate(TemplateInfo template, out JObject serializedTemplate)
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

                if (JsonSerializerHelpers.TrySerializeDictionary(template.Tags, JsonSerializerHelpers.StringKeyConverter, SerializeCacheTag, out JObject tagListObject)
                        && tagListObject != null)
                {
                    serializedTemplate.Add(nameof(TemplateInfo.Tags), tagListObject);
                }
                else
                {
                    serializedTemplate = null;
                    return false;
                }

                if (JsonSerializerHelpers.TrySerializeDictionary(template.CacheParameters, JsonSerializerHelpers.StringKeyConverter, SerializeCacheParameter, out JObject cacheParamObject)
                        && cacheParamObject != null)
                {
                    serializedTemplate.Add(nameof(TemplateInfo.CacheParameters), cacheParamObject);
                }
                else
                {
                    serializedTemplate = null;
                    return false;
                }

                // Note: Parameters are not serialized. Everything needed from them is in Tags & CacheParameters

                serializedTemplate.Add(nameof(TemplateInfo.ConfigPlace), template.ConfigPlace);
                serializedTemplate.Add(nameof(TemplateInfo.LocaleConfigMountPointId), template.LocaleConfigMountPointId);
                serializedTemplate.Add(nameof(TemplateInfo.LocaleConfigPlace), template.LocaleConfigPlace);
                serializedTemplate.Add(nameof(TemplateInfo.HostConfigMountPointId), template.HostConfigMountPointId);
                serializedTemplate.Add(nameof(TemplateInfo.HostConfigPlace), template.HostConfigPlace);
                serializedTemplate.Add(nameof(TemplateInfo.ThirdPartyNotices), template.ThirdPartyNotices);

                if (JsonSerializerHelpers.TrySerializeDictionary(template.BaselineInfo, JsonSerializerHelpers.StringKeyConverter, SerializeBaseline, out JObject baselineInfoObject)
                        && baselineInfoObject != null)
                {
                    serializedTemplate.Add(nameof(TemplateInfo.BaselineInfo), baselineInfoObject);
                }
                else
                {
                    serializedTemplate = null;
                    return false;
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

        // TODO: after IAllowDefaultIfOptionWithoutValue is rolled up into ICacheParameter, get rid of the extra check for it.
        private static Func<ICacheTag, JObject> SerializeCacheTag = tag =>
        {
            JObject tagObject = new JObject();
            tagObject.Add(nameof(ICacheTag.Description), tag.Description);

            if (JsonSerializerHelpers.TrySerializeStringDictionary(tag.ChoicesAndDescriptions, out JObject serializedChoicesAndDescriptions))
            {
                tagObject.Add(nameof(ICacheTag.ChoicesAndDescriptions), serializedChoicesAndDescriptions);
            }
            else
            {
                tagObject = null;
            }

            tagObject.Add(nameof(ICacheTag.DefaultValue), tag.DefaultValue);

            if (tag is IAllowDefaultIfOptionWithoutValue tagWithNoValueDefault
                    && !string.IsNullOrEmpty(tagWithNoValueDefault.DefaultIfOptionWithoutValue))
            {
                tagObject.Add(nameof(IAllowDefaultIfOptionWithoutValue.DefaultIfOptionWithoutValue), tagWithNoValueDefault.DefaultIfOptionWithoutValue);
            }

            return tagObject;
        };

        // TODO: after IAllowDefaultIfOptionWithoutValue is rolled up into ICacheParameter, get rid of the extra check for it.
        private static Func<ICacheParameter, JObject> SerializeCacheParameter = param =>
        {
            JObject paramObject = new JObject();
            paramObject.Add(nameof(ICacheParameter.DataType), param.DataType);
            paramObject.Add(nameof(ICacheParameter.DefaultValue), param.DefaultValue);
            paramObject.Add(nameof(ICacheParameter.Description), param.Description);

            if (param is IAllowDefaultIfOptionWithoutValue paramWithNoValueDefault
                && !string.IsNullOrEmpty(paramWithNoValueDefault.DefaultIfOptionWithoutValue))
            {
                paramObject.Add(nameof(IAllowDefaultIfOptionWithoutValue.DefaultIfOptionWithoutValue), paramWithNoValueDefault.DefaultIfOptionWithoutValue);
            }

            return paramObject;
        };

        private static Func<IBaselineInfo, JObject> SerializeBaseline = baseline =>
        {
            JObject baselineObject = new JObject();
            baselineObject.Add(nameof(IBaselineInfo.Description), baseline.Description);
            if (JsonSerializerHelpers.TrySerializeStringDictionary(baseline.DefaultOverrides, out JObject defaultsObject))
            {
                baselineObject.Add(nameof(IBaselineInfo.DefaultOverrides), defaultsObject);
            }
            else
            {
                baselineObject = null;
            }

            return baselineObject;
        };
    }
}
