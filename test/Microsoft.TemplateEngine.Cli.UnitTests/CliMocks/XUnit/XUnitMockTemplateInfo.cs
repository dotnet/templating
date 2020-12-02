using Microsoft.TemplateEngine.Mocks;
using Newtonsoft.Json;
using System.Collections.Generic;
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
            Description = info.GetValue<string>("template_description");

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
            info.AddValue("template_description", Description, typeof(string));

            info.AddValue("template_tags", JsonConvert.SerializeObject(_tags), typeof(string));
            info.AddValue("template_params", JsonConvert.SerializeObject(_cacheParameters), typeof(string));
        }

        public override string ToString()
        {
            return "Identity: " + Identity + ";Group: " + GroupIdentity + ";ShortName: " + ShortName + ";Precedence: " + Precedence;
        }

        public new XUnitMockTemplateInfo WithParameters(params string[] parameters)
        {
            base.WithParameters(parameters);
            return this;
        }
        public new XUnitMockTemplateInfo WithTag(string tagName, params string[] values)
        {
            base.WithTag(tagName, values);
            return this;
        }
    }
}
