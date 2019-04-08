using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateSearch.TemplateDiscovery
{
    public class ScraperConfig
    {
        public string BasePath { get; set; }
        public int PageSize { get; set; }
        public bool RunOnlyOnePage { get; set; }
        public string PreviousRunBasePath { get; set; }
    }
}
