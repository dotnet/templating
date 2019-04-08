using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource
{
    public class NuGetSearchCacheConfig : ISearchCacheConfig
    {
        public static readonly string _cliHostDataName = "cliHostData";

        public NuGetSearchCacheConfig(string templateDiscoveryFileName)
        {
            TemplateDiscoveryFileName = templateDiscoveryFileName;

            AdditionalDataReaders = new Dictionary<string, Func<JObject, object>>()
            {
                { _cliHostDataName, CliHostDataReader }
            };
        }

        public string TemplateDiscoveryFileName { get; }

        public IReadOnlyDictionary<string, Func<JObject, object>> AdditionalDataReaders { get; }

        private static readonly Func<JObject, object> CliHostDataReader = (cacheObject) =>
        {
            try
            {
                return cacheObject.ToObject<Dictionary<string, HostSpecificTemplateData>>();
            }
            catch (Exception ex)
            {
                throw new Exception("Error deserializing the cli host specific template data.", ex);
            }
        };
    }
}
