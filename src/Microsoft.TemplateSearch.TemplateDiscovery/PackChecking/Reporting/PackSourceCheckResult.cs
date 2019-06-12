﻿using System.Collections.Generic;
using Microsoft.TemplateSearch.TemplateDiscovery.AdditionalData;

namespace Microsoft.TemplateSearch.TemplateDiscovery.PackChecking.Reporting
{
    public class PackSourceCheckResult
    {
        public PackSourceCheckResult(IReadOnlyList<PackCheckResult> packCheckData, IReadOnlyList<IAdditionalDataProducer> additionalDataHandlers)
        {
            PackCheckData = packCheckData;
            AdditionalDataProducers = additionalDataHandlers;
        }

        public IReadOnlyList<PackCheckResult> PackCheckData { get; }

        public IReadOnlyList<IAdditionalDataProducer> AdditionalDataProducers { get; }
    }
}
