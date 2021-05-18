// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Microsoft.TemplateEngine.Mocks
{
    public class MockTemplateInfo : ITemplateInfo, IXunitSerializable
    {
        private string[] _parameters = Array.Empty<string>();

        private string[] _baselineInfo = Array.Empty<string>();

        private string[] _classifications = Array.Empty<string>();

        private string[] _shortNameList = Array.Empty<string>();

        private Dictionary<string, string> _tags = new Dictionary<string, string>();

        private Dictionary<string, string[]> _choiceParameters = new Dictionary<string, string[]>();

        public MockTemplateInfo()
        {
        }

        public MockTemplateInfo(string shortName, string name = null, string identity = null, string groupIdentity = null, int precedence = 0, string author = null)
            : this(new string[] { shortName }, name, identity, groupIdentity, precedence, author)
        {
        }

        public MockTemplateInfo(string[] shortNames, string name = null, string identity = null, string groupIdentity = null, int precedence = 0, string author = null) : this()
        {
            _shortNameList = shortNames;
            if (string.IsNullOrEmpty(name))
            {
                Name = "Template " + shortNames[0];
            }
            else
            {
                Name = name;
            }

            if (string.IsNullOrEmpty(identity))
            {
                Identity = shortNames[0];
            }
            else
            {
                Identity = identity;
            }

            Precedence = precedence;
            GroupIdentity = groupIdentity;
            Identity = identity;
            Author = author;
        }

        public string Author { get; private set; }

        public string Description { get; private set; }

        public IReadOnlyList<string> Classifications
        {
            get
            {
                return _classifications;
            }
        }

        public string DefaultName { get; }

        public string Identity { get; private set; }

        public Guid GeneratorId { get; }

        public string GroupIdentity { get; private set; }

        public int Precedence { get; private set; }

        public string Name { get; private set; }

        public string ShortName
        {
            get
            {
                if (_shortNameList.Length > 0)
                {
                    return _shortNameList[0];
                }
                return string.Empty;
            }
        }

        public IReadOnlyList<string> ShortNameList => _shortNameList;

        [Obsolete("Use Parameters instead.")]
        IReadOnlyDictionary<string, ICacheTag> ITemplateInfo.Tags => throw new NotImplementedException();

        [Obsolete("Use Parameters instead.")]
        IReadOnlyDictionary<string, ICacheParameter> ITemplateInfo.CacheParameters => throw new NotImplementedException();

        public IReadOnlyList<ITemplateParameter> Parameters
        {
            get
            {
                List<ITemplateParameter> parameters = new List<ITemplateParameter>();
                foreach (var param in _parameters)
                {
                    parameters.Add(new TemplateParameter(param, "parameter", "string"));
                }
                foreach (var param in _choiceParameters)
                {
                    parameters.Add(new TemplateParameter(
                        param.Key,
                        type: "parameter",
                        datatype: "choice",
                        choices: param.Value.ToDictionary(v => v, v => new ParameterChoice(null, null))));

                }
                return parameters;
            }            
        }

        public string MountPointUri { get; }

        public string ConfigPlace { get; }

        public string LocaleConfigPlace { get; }

        public string HostConfigPlace { get; }

        public string ThirdPartyNotices { get; }

        public IReadOnlyDictionary<string, IBaselineInfo> BaselineInfo
        {
            get
            {
                return _baselineInfo.ToDictionary(k => k, k => (IBaselineInfo)new BaselineCacheInfo());
            }
        }

        bool ITemplateInfo.HasScriptRunningPostActions { get; set; }

        public DateTime? ConfigTimestampUtc { get; }

        public IReadOnlyDictionary<string, string> TagsCollection => _tags;

        public MockTemplateInfo WithParameters(params string[] parameters)
        {
            if (_parameters.Length == 0)
            {
                _parameters = parameters;
            }
            else
            {
                _parameters = _parameters.Concat(parameters).ToArray();
            }
            return this;
        }

        public MockTemplateInfo WithTag(string tagName,  string value)
        {
            _tags.Add(tagName, value);
            return this;
        }

        public MockTemplateInfo WithChoiceParameter(string name, params string[] values)
        {
            _choiceParameters.Add(name, values);
            return this;
        }

        public MockTemplateInfo WithDescription(string description)
        {
            Description = description;
            return this;
        }

        public MockTemplateInfo WithBaselineInfo(params string[] baseline)
        {
            if (_baselineInfo.Length == 0)
            {
                _baselineInfo = baseline;
            }
            else
            {
                _baselineInfo = _baselineInfo.Concat(baseline).ToArray();
            }
            return this;
        }

        public MockTemplateInfo WithClassifications(params string[] classifications)
        {
            if (_classifications.Length == 0)
            {
                _classifications = classifications;
            }
            else
            {
                _classifications = _classifications.Concat(classifications).ToArray();
            }
            return this;
        }

        #region XUnitSerializable implementation

        public void Deserialize(IXunitSerializationInfo info)
        {
            Name = info.GetValue<string>("template_name");
            Precedence = info.GetValue<int>("template_precedence");
            Identity = info.GetValue<string>("template_identity");
            GroupIdentity = info.GetValue<string>("template_group");
            Description = info.GetValue<string>("template_description");
            Author = info.GetValue<string>("template_author");

            _choiceParameters = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(info.GetValue<string>("template_choices"));
            _tags = JsonConvert.DeserializeObject<Dictionary<string, string>>(info.GetValue<string>("template_tags"));
            _parameters = JsonConvert.DeserializeObject<string[]>(info.GetValue<string>("template_params"));
            _baselineInfo = JsonConvert.DeserializeObject<string[]>(info.GetValue<string>("template_baseline"));
            _classifications = JsonConvert.DeserializeObject<string[]>(info.GetValue<string>("template_classifications"));
            _shortNameList = JsonConvert.DeserializeObject<string[]>(info.GetValue<string>("template_shortname"));
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue("template_name", Name, typeof(string));
            info.AddValue("template_shortname", JsonConvert.SerializeObject(_shortNameList), typeof(string));
            info.AddValue("template_precedence", Precedence, typeof(int));
            info.AddValue("template_identity", Identity, typeof(string));
            info.AddValue("template_group", GroupIdentity, typeof(string));
            info.AddValue("template_description", Description, typeof(string));
            info.AddValue("template_author", Author, typeof(string));

            info.AddValue("template_choices", JsonConvert.SerializeObject(_choiceParameters), typeof(string));
            info.AddValue("template_tags", JsonConvert.SerializeObject(_tags), typeof(string));
            info.AddValue("template_params", JsonConvert.SerializeObject(_parameters), typeof(string));
            info.AddValue("template_baseline", JsonConvert.SerializeObject(_baselineInfo), typeof(string));
            info.AddValue("template_classifications", JsonConvert.SerializeObject(_classifications), typeof(string));
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            _ = sb.Append("Short name:" + string.Join(",", _shortNameList) + ";");
            _ = sb.Append("Identity:" + Identity + ";");

            if (string.IsNullOrEmpty(GroupIdentity))
            {
                _ = sb.Append("Group:<not set>;");
            }
            else
            {
                _ = sb.Append("Group:" + GroupIdentity + ";");
            }
            if (Precedence != 0)
            {
                _ = sb.Append("Precedence:" + Precedence + ";");
            }
            if (!string.IsNullOrEmpty(Author))
            {
                _ = sb.Append("Author:" + Author + ";");
            }
            if (Classifications.Any())
            {
                _ = sb.Append("Classifications:" + string.Join(",", _classifications) + ";");
            }
            if (_parameters.Any())
            {
                _ = sb.Append("Parameters:" + string.Join(",", _parameters) + ";");
            }
            if (_baselineInfo.Any())
            {
                _ = sb.Append("Baseline:" + string.Join(",", _baselineInfo) + ";");
            }
            if (_tags.Any())
            {
                _ = sb.Append("Tags:" + string.Join(",", _tags.Select(t => t.Key + "(" + t.Value + ")")) + ";");
            }
            if (_choiceParameters.Any())
            {
                _ = sb.Append("Choice parameters:" + string.Join(",", _choiceParameters.Select(t => t.Key + "(" + string.Join("|", t.Value) + ")")) + ";");
            }

            return sb.ToString();
        }

        #endregion XUnitSerializable implementation
    }
}
