using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Utils
{
    public class OutputValue : IOutputValue
    {
        public OutputValue(string inputPath, string outputPath, string name, object value)
        {
            InputPath = inputPath;
            OutputPath = outputPath;
            Name = name;
            Value = value;
        }

        public string InputPath { get; }

        public string OutputPath { get; }

        public string Name { get; }

        public object Value { get; }
    }
}
