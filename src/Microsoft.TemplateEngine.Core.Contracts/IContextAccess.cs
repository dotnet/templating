using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Core.Contracts
{
    public interface IContextAccess
    {
        IEngineEnvironmentSettings EnvironmentSettings { get; }

        void SetContext(IEngineEnvironmentSettings settings, string inputFile, string outputFile);

        string InputFile { get; }

        string OutputFile { get; }
    }
}
