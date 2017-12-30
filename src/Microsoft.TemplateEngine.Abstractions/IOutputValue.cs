namespace Microsoft.TemplateEngine.Abstractions
{
    public interface IOutputValue
    {
        string InputPath { get; }

        string OutputPath { get; }

        string Name { get; }

        object Value { get; }
    }
}
