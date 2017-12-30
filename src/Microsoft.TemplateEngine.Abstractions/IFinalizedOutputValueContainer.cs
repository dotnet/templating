using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions
{
    public interface IFinalizedOutputValueContainer
    {
        IEnumerable<IOutputValue> GetValuesByOutputFile(string outputFilePath);

        IEnumerable<IOutputValue> GetValuesByInputFile(string inputFilePath);

        IEnumerable<IOutputValue> GetValuesByName(string name);
    }
}
