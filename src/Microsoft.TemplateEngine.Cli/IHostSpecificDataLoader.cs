using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Json;

namespace Microsoft.TemplateEngine.Cli
{
    public interface IHostSpecificDataLoader
    {
        HostSpecificTemplateData ReadHostSpecificTemplateData(ITemplateInfo templateInfo);
    }
}
