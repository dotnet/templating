using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    public interface ITemplateNameSearchResult
    {
        ITemplateInfo Template { get; }

        HostSpecificTemplateData HostSpecificTemplateData { get; }

        PackAndVersion PackInfo { get; }
    }
}
