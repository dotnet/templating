// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateSearch.Common.Abstractions;
using Microsoft.TemplateSearch.TemplateDiscovery.PackChecking;

namespace Microsoft.TemplateSearch.TemplateDiscovery.AdditionalData
{
    internal class CliHostDataProducer : IAdditionalDataProducer
    {
        private const string CliHostDataName = "cliHostData";

        private Dictionary<string, CliHostTemplateData> _hostDataForPackByTemplate = new(StringComparer.OrdinalIgnoreCase);

        private Dictionary<ITemplatePackageInfo, Dictionary<string, CliHostTemplateData>> _hostDataForPack = new(new ITemplatePackageInfoComparer());

        internal CliHostDataProducer()
        {
            _hostDataForPackByTemplate = new Dictionary<string, CliHostTemplateData>();
        }

        public string DataUniqueName => CliHostDataName;

        public object? Data => _hostDataForPackByTemplate;

        public object? CreateDataForTemplate(ITemplateInfo template, IEngineEnvironmentSettings environment)
        {
            CliHostTemplateDataLoader hostDataLoader = new CliHostTemplateDataLoader(environment);
            CliHostTemplateData hostData = hostDataLoader.ReadHostSpecificTemplateData(template);
            // store the host data if it has any info that could affect searching for this template.
            if (hostData.IsHidden || hostData.SymbolInfo.Count > 0)
            {
                return hostData;
            }
            return null;
        }

        public void CreateDataForTemplatePack(IDownloadedPackInfo packInfo, IReadOnlyList<ITemplateInfo> templateList, IEngineEnvironmentSettings environment)
        {
            CliHostTemplateDataLoader hostDataLoader = new CliHostTemplateDataLoader(environment);
            Dictionary<string, CliHostTemplateData> dataForPack = new Dictionary<string, CliHostTemplateData>(StringComparer.OrdinalIgnoreCase);

            foreach (ITemplateInfo template in templateList)
            {
                CliHostTemplateData hostData = hostDataLoader.ReadHostSpecificTemplateData(template);

                // store the host data if it has any info that could affect searching for this template.
                if (hostData.IsHidden || hostData.SymbolInfo.Count > 0)
                {
                    _hostDataForPackByTemplate[template.Identity] = hostData;
                    dataForPack[template.Identity] = hostData;
                }
            }
            _hostDataForPack[packInfo] = dataForPack;
        }

        public object? CreateDataForTemplatePackage(ITemplatePackageInfo packInfo) => null;

        private class ITemplatePackageInfoComparer : IEqualityComparer<ITemplatePackageInfo>
        {
            public bool Equals(ITemplatePackageInfo? x, ITemplatePackageInfo? y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return x.Name.Equals(y.Name, StringComparison.OrdinalIgnoreCase)
                    && (x.Version?.Equals(y.Version, StringComparison.OrdinalIgnoreCase) ?? x.Version == y.Version);
            }

            public int GetHashCode([DisallowNull] ITemplatePackageInfo obj)
            {
                return new
                {
                    a = obj.Version?.ToLowerInvariant(),
                    b = obj.Name.ToLowerInvariant()
                }.GetHashCode();
            }
        }
    }
}
