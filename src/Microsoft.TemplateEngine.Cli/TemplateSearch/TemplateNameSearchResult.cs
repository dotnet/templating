using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    public class TemplateNameSearchResult : ITemplateNameSearchResult
    {
        public TemplateNameSearchResult(ITemplateInfo template, string packName)
            : this(template, HostSpecificTemplateData.Default, packName)
        {
        }

        public TemplateNameSearchResult(ITemplateInfo template, HostSpecificTemplateData hostSpecificData, string packName)
        {
            Template = template;
            HostSpecificTemplateData = hostSpecificData;
            PackName = packName;
        }

        public ITemplateInfo Template { get; }

        public HostSpecificTemplateData HostSpecificTemplateData { get; }

        public string PackName { get; }
    }
}
