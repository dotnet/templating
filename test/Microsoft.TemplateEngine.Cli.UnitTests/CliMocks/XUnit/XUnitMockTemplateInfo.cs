using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.UnitTests.TemplateResolutionTests;
using Microsoft.TemplateEngine.Mocks;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;

namespace Microsoft.TemplateEngine.Cli.UnitTests.CliMocks
{
    public class XUnitMockTemplateInfo : MockTemplateInfo, IXunitSerializable
    {
        public XUnitMockTemplateInfo()
        {

        }
        public XUnitMockTemplateInfo(string shortName, string name = null, string identity = null, string groupIdentity = null, int precedence = 0)
        { 
            ShortName = shortName;
            if (string.IsNullOrEmpty(name))
            {
                Name = "Template " + shortName;
            }
            else
            {
                Name = name;
            }

            if (string.IsNullOrEmpty(identity))
            {
                Identity = shortName;
            }
            else
            {
                Identity = identity;
            }

            Precedence = precedence;
            GroupIdentity = groupIdentity;
            Identity = identity;
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Name = info.GetValue<string>("template_name");
            ShortName = info.GetValue<string>("template_shortname");
            Precedence = info.GetValue<int>("template_precedence");
            Identity = info.GetValue<string>("template_identity");
            GroupIdentity = info.GetValue<string>("template_group");
            _tags = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(info.GetValue<string>("template_tags"));
            _cacheParameters = JsonConvert.DeserializeObject<string[]>(info.GetValue<string>("template_params"));
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue("template_name", Name, typeof(string));
            info.AddValue("template_shortname", ShortName, typeof(string));
            info.AddValue("template_precedence", Precedence, typeof(int));
            info.AddValue("template_identity", Identity, typeof(string));
            info.AddValue("template_group", GroupIdentity, typeof(string));

            info.AddValue("template_tags", JsonConvert.SerializeObject(_tags), typeof(string));
            info.AddValue("template_params", JsonConvert.SerializeObject(_cacheParameters), typeof(string));
        }

        public override string ToString()
        {
            return "Identity: " + Identity + ";Group: " + GroupIdentity + ";ShortName: " + ShortName + ";Precedence: " + Precedence;
        }

        public XUnitMockTemplateInfo WithParameters(params string [] parameters)
        {
            _cacheParameters = parameters;
            return this;
        }
        public XUnitMockTemplateInfo WithTag(string tagName, params string[] values)
        {
            _tags.Add(tagName, values);
            return this;
        }

        private string[] _cacheParameters = Array.Empty<string>();
        private Dictionary<string, string[]> _tags = new Dictionary<string, string[]>();


        public override IReadOnlyDictionary<string, ICacheTag> Tags
        {
            get
            {
                return _tags.ToDictionary(kvp => kvp.Key, kvp => ResolutionTestHelper.CreateTestCacheTag(kvp.Value));
            }
        }

        public override IReadOnlyDictionary<string, ICacheParameter> CacheParameters
        {
            get
            {
                return _cacheParameters.ToDictionary(param => param, kvp => (ICacheParameter) new CacheParameter());
            }
        }
    }
}
