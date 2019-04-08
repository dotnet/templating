using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    public class TemplateNameSearchResult : ITemplateNameSearchResult
    {
        public TemplateNameSearchResult(ITemplateInfo template, PackAndVersion packInfo)
            : this(template, HostSpecificTemplateData.Default, packInfo)
        {
        }

        public TemplateNameSearchResult(ITemplateInfo template, HostSpecificTemplateData hostSpecificData, PackAndVersion packInfo)
        {
            Template = template;
            HostSpecificTemplateData = hostSpecificData;
            PackInfo = packInfo;
        }

        public ITemplateInfo Template { get; }

        public HostSpecificTemplateData HostSpecificTemplateData { get; }

        public PackAndVersion PackInfo { get; }
    }
}
