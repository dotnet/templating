using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions
{
    public interface IOutputValuesContainer : IEnumerable<IOutputValue>
    {
        void MapFile(string inputFile, string outputFile);

        object this[string inputPath, string outputPath, string name] { get; set; }

        int Count { get; }
    }
}
