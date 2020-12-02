using Microsoft.TemplateEngine.Cli.HelpAndUsage;
using Xunit.Abstractions;

namespace Microsoft.TemplateEngine.Cli.UnitTests.CliMocks.XUnit
{
    internal class XUnitInvalidParameterInfo : IXunitSerializable
    {

        public XUnitInvalidParameterInfo() 
        {

        }

        public XUnitInvalidParameterInfo(InvalidParameterInfoKind kind, string inputFormat, string specifiedValue)
        {
            Kind = kind;
            InputFormat = inputFormat;
            SpecifiedValue = specifiedValue;
        }

        public string InputFormat { get; private set; }
        public string SpecifiedValue { get; private set; }
        public InvalidParameterInfoKind Kind { get; private set; }

        public void Deserialize(IXunitSerializationInfo info)
        {
            Kind = (InvalidParameterInfoKind) info.GetValue<int>("param_kind");
            InputFormat = info.GetValue<string>("param_input_format");
            SpecifiedValue = info.GetValue<string>("param_value");
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue("param_kind", (int) Kind, typeof(int));
            info.AddValue("param_input_format", InputFormat, typeof(string));
            info.AddValue("param_value", SpecifiedValue, typeof(string));
        }
    }
}
