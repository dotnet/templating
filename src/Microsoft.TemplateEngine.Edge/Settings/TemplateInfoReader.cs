// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    internal class TemplateInfoReader
    {
        internal static TemplateInfo FromJObject(JObject entry)
        {
            TemplateInfo info = new TemplateInfo();

            info.MountPointUri = entry.ToString(nameof(TemplateInfo.MountPointUri));
            info.Author = entry.ToString(nameof(TemplateInfo.Author));
            JArray classificationsArray = entry.Get<JArray>(nameof(TemplateInfo.Classifications));

            List<string> classifications = new List<string>();
            foreach (JToken item in classificationsArray)
            {
                classifications.Add(item.ToString());
            }
            info.Classifications = classifications;

            info.DefaultName = entry.ToString(nameof(TemplateInfo.DefaultName));
            info.Description = entry.ToString(nameof(TemplateInfo.Description));
            info.Identity = entry.ToString(nameof(TemplateInfo.Identity));
            info.GeneratorId = Guid.Parse(entry.ToString(nameof(TemplateInfo.GeneratorId)));
            info.GroupIdentity = entry.ToString(nameof(TemplateInfo.GroupIdentity));
            info.Precedence = entry.ToInt32(nameof(TemplateInfo.Precedence));
            info.Name = entry.ToString(nameof(TemplateInfo.Name));

            JToken shortNameToken = entry.Get<JToken>(nameof(TemplateInfo.ShortNameList));
            info.ShortNameList = JTokenStringOrArrayToCollection(shortNameToken, System.Array.Empty<string>());

            info.ConfigPlace = entry.ToString(nameof(TemplateInfo.ConfigPlace));
            info.LocaleConfigPlace = entry.ToString(nameof(TemplateInfo.LocaleConfigPlace));
            info.HostConfigPlace = entry.ToString(nameof(TemplateInfo.HostConfigPlace));
            info.ThirdPartyNotices = entry.ToString(nameof(TemplateInfo.ThirdPartyNotices));

            JObject baselineJObject = entry.Get<JObject>(nameof(ITemplateInfo.BaselineInfo));
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
            info.BaselineInfo = baselineInfo;

            //read parameters
            List<ITemplateParameter> templateParameters = new List<ITemplateParameter>();
            JArray parametersArray = entry.Get<JArray>(nameof(TemplateInfo.Parameters));
            if (parametersArray != null)
            {
                foreach (JObject item in parametersArray)
                {
                    templateParameters.Add(new TemplateParameter(item));
                }
            }
            info.Parameters = templateParameters;

            //read tags
            // tags are just "name": "description"
            // e.g.: "language": "C#"
            JObject tagsObject = entry.Get<JObject>(nameof(TemplateInfo.TagsCollection));
            Dictionary<string, string> tags = new Dictionary<string, string>();
            info.TagsCollection = tags;
            if (tagsObject != null)
            {
                foreach (JProperty item in tagsObject.Properties())
                {
                    tags.Add(item.Name.ToString(), item.Value.ToString());
                }
            }
            return info;
        }

        private static IReadOnlyList<string> JTokenStringOrArrayToCollection(JToken token, string[] defaultSet)
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
    }
}
