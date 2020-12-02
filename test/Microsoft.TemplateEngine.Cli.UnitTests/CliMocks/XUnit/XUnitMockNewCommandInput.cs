using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;

namespace Microsoft.TemplateEngine.Cli.UnitTests.CliMocks.XUnit
{
    internal class XUnitMockNewCommandInput : MockNewCommandInput, IXunitSerializable
    {
        public XUnitMockNewCommandInput()
        {

        }

        public XUnitMockNewCommandInput(string templateName, string language = null) : base(templateName, language)
        {

        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            TemplateName = info.GetValue<string>("command_templateName");
            Language = info.GetValue<string>("command_language");
            _rawParameterInputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(info.GetValue<string>("command_rawParameters"));
        }
        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue("command_templateName", TemplateName, typeof(string));
            info.AddValue("command_language", Language, typeof(string));
            info.AddValue("command_rawParameters", JsonConvert.SerializeObject(_rawParameterInputs), typeof(string));
        }

        public XUnitMockNewCommandInput WithOption (string optionName, string optionValue = null)
        {
            _rawParameterInputs[optionName] = optionValue;
            return this;
        }

        public override string ToString()
        {
            string result = TemplateName;

            if (!string.IsNullOrEmpty(Language))
            {
                result = result + " --language " + Language;
            }

            result += " " + string.Join(" ", _rawParameterInputs.Select(kvp => kvp.Key + " " + kvp.Value));
            return result;
        }
    }
}
