using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    public interface ITemplateNameSearchResult
    {
        ITemplateInfo Template { get; }

        HostSpecificTemplateData HostSpecificTemplateData { get; }

        string PackName { get; }
    }
}
