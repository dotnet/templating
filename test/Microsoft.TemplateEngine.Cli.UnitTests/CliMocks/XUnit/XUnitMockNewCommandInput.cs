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
            TypeFilter = info.GetValue<string>("command_type");
            PackageFilter = info.GetValue<string>("command_package");
            IsListFlagSpecified = info.GetValue<bool>("command_list");
            IsHelpFlagSpecified = info.GetValue<bool>("command_help");
            ShowAllColumns = info.GetValue<bool>("command_columns_all");
            BaselineName = info.GetValue<string>("command_baseline");
            AuthorFilter = info.GetValue<string>("command_author");
            Columns = info.GetValue<string[]>("command_columns");
            _rawParameterInputs = JsonConvert.DeserializeObject<Dictionary<string, string>>(info.GetValue<string>("command_rawParameters"));
        }
        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue("command_templateName", TemplateName, typeof(string));
            info.AddValue("command_language", Language, typeof(string));
            info.AddValue("command_type", TypeFilter, typeof(string));
            info.AddValue("command_package", PackageFilter, typeof(string));
            info.AddValue("command_author", AuthorFilter, typeof(string));
            info.AddValue("command_baseline", BaselineName, typeof(string));
            info.AddValue("command_list", IsListFlagSpecified, typeof(bool));
            info.AddValue("command_help", IsHelpFlagSpecified, typeof(bool));
            info.AddValue("command_columns_all", ShowAllColumns, typeof(bool));
            info.AddValue("command_columns", Columns.ToArray(), typeof(string[]));
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
