using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Cli;
using Microsoft.TemplateSearch.TemplateDiscovery.PackProviders;
using Newtonsoft.Json;

namespace Microsoft.TemplateSearch.TemplateDiscovery.AdditionalData
{
    public class CliHostDataProducer : IAdditionalDataProducer
    {
        private static readonly string CliHostDataName = "cliHostData";

        private Dictionary<string, HostSpecificTemplateData> _hostDataForPackByTemplate = new Dictionary<string, HostSpecificTemplateData>();

        public CliHostDataProducer()
        {
            _hostDataForPackByTemplate = new Dictionary<string, HostSpecificTemplateData>();
        }

        public string DataUniqueName => CliHostDataName;

        public void CreateDataForTemplatePack(IDownloadedPackInfo packInfo, IReadOnlyList<ITemplateInfo> templateList, IEngineEnvironmentSettings environment)
        {
            IHostSpecificDataLoader hostDataLoader = new HostSpecificDataLoader(environment.SettingsLoader);

            foreach (ITemplateInfo template in templateList)
            {
                HostSpecificTemplateData hostData = hostDataLoader.ReadHostSpecificTemplateData(template);

                // store the host data if it has any info that could affect searching for this template.
                if (hostData.IsHidden || hostData.SymbolInfo.Count > 0)
                {
                    _hostDataForPackByTemplate[template.Identity] = hostData;
                }
            }
        }

        public string Serialized => JsonConvert.SerializeObject(_hostDataForPackByTemplate, Formatting.Indented);

        public object Data => _hostDataForPackByTemplate;
    }
}
