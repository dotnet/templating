﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateSearch.Common.Abstractions;

namespace Microsoft.TemplateSearch.Common.Providers
{
    public sealed class NuGetMetadataSearchProviderFactory : ITemplateSearchProviderFactory
    {
        string ITemplateSearchProviderFactory.DisplayName => "NuGet.org";

        Guid IIdentifiedComponent.Id => new Guid("6EA368C4-8A56-444C-91D1-55150B296BF2");

        ITemplateSearchProvider ITemplateSearchProviderFactory.CreateProvider(
            IEngineEnvironmentSettings environmentSettings,
            IReadOnlyDictionary<string, Func<object, object>> additionalDataReaders)
        {
            if (environmentSettings is null)
            {
                throw new ArgumentNullException(nameof(environmentSettings));
            }

            if (additionalDataReaders is null)
            {
                throw new ArgumentNullException(nameof(additionalDataReaders));
            }

            return new NuGetMetadataSearchProvider(this, environmentSettings, additionalDataReaders);
        }
    }
}
