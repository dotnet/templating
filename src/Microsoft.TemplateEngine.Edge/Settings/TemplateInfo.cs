// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    internal class TemplateInfo : ITemplateInfo
    {
        internal const string CurrentVersion = "1.0.0.5";

#pragma warning disable CS0618 // Type or member is obsolete
        private IReadOnlyDictionary<string, ICacheTag> _tags;
        private IReadOnlyDictionary<string, ICacheParameter> _cacheParameters;
#pragma warning restore CS0618 // Type or member is obsolete

        internal TemplateInfo()
        {
            ShortNameList = new List<string>();
        }

        [JsonProperty]
        public IReadOnlyList<ITemplateParameter> Parameters { get; set; }

        [JsonProperty]
        public string MountPointUri { get; set; }

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

            set
            {
                if (ShortNameList.Count > 0)
                {
                    throw new Exception("Can't set the short name when the ShortNameList already has entries.");
                }

                ShortNameList = new List<string>() { value };
            }
        }

        public IReadOnlyList<string> ShortNameList { get; set; }

        [JsonIgnore]
        [Obsolete]
        public IReadOnlyDictionary<string, ICacheTag> Tags
        {
            get
            {
                if (_tags == null)
                {
                    Dictionary<string, ICacheTag> tags = new Dictionary<string, ICacheTag>();
                    foreach (KeyValuePair<string, string> tag in TagsCollection)
                    {
                        tags[tag.Key] = new CacheTag(null, null, new Dictionary<string, ParameterChoice> { { tag.Value, new ParameterChoice(null, null) } }, tag.Value);
                    }
                    foreach (ITemplateParameter parameter in Parameters.Where(p => p.DataType.Equals("choice", StringComparison.OrdinalIgnoreCase)))
                    {
                        tags[parameter.Name] = new CacheTag(parameter.DisplayName, parameter.Documentation, parameter.Choices, parameter.DefaultValue);
                    }
                    return _tags = tags;
                }
                return _tags;
            }
        }

        [JsonIgnore]
        [Obsolete]
        public IReadOnlyDictionary<string, ICacheParameter> CacheParameters
        {
            get
            {
                if (_cacheParameters == null)
                {
                    Dictionary<string, ICacheParameter> cacheParameters = new Dictionary<string, ICacheParameter>();
                    foreach (ITemplateParameter parameter in Parameters.Where(p => !p.DataType.Equals("choice", StringComparison.OrdinalIgnoreCase)))
                    {
                        cacheParameters[parameter.Name] = new CacheParameter()
                        {
                             DataType = parameter.DataType,
                             DefaultValue = parameter.DefaultValue,
                             Description = parameter.Documentation,
                             DefaultIfOptionWithoutValue = parameter.DefaultIfOptionWithoutValue,
                             DisplayName = parameter.DisplayName

                        };
                    }
                    return _cacheParameters = cacheParameters;
                }
                return _cacheParameters;
            }
        }

        [JsonProperty]
        public string ConfigPlace { get; set; }

        [JsonProperty]
        public string LocaleConfigPlace { get; set; }

        [JsonProperty]
        public string HostConfigPlace { get; set; }

        [JsonProperty]
        public string ThirdPartyNotices { get; set; }

        [JsonProperty]
        public IReadOnlyDictionary<string, IBaselineInfo> BaselineInfo { get; set; }

        [JsonProperty]
        public IReadOnlyDictionary<string, string> TagsCollection { get; set; }

        [JsonIgnore]
        bool ITemplateInfo.HasScriptRunningPostActions { get; set; }

        public static TemplateInfo FromJObject(JObject entry)
        {
            return TemplateInfoReader.FromJObject(entry);
        }

    }
}
