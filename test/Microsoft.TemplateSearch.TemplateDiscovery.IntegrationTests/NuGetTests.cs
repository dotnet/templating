﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateSearch.TemplateDiscovery.NuGet;
using Newtonsoft.Json.Linq;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Xunit;

namespace Microsoft.TemplateSearch.TemplateDiscovery.IntegrationTests
{
    public class NuGetTests
    {
        [Fact]
        public async Task CanReadPackageInfo()
        {
            string nuGetOrgFeed = "https://api.nuget.org/v3/index.json";
            var repository = Repository.Factory.GetCoreV3(nuGetOrgFeed);
            ServiceIndexResourceV3 indexResource = repository.GetResource<ServiceIndexResourceV3>();
            IReadOnlyList<ServiceIndexEntry> searchResources = indexResource.GetServiceEntries("SearchQueryService");
            string queryString = $"{searchResources[0].Uri}?q=Microsoft.DotNet.Common.ProjectTemplates.5.0&skip=0&take=10&prerelease=true&semVerLevel=2.0.0";
            Uri queryUri = new Uri(queryString);
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(queryUri, CancellationToken.None).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                {
                    string responseText = await response.Content.ReadAsStringAsync(CancellationToken.None).ConfigureAwait(false);

                    NuGetPackageSearchResult resultsForPage = NuGetPackageSearchResult.FromJObject(JObject.Parse(responseText));
                    Assert.Equal(1, resultsForPage.TotalHits);
                    Assert.Equal(1, resultsForPage.Data.Count);

                    var packageInfo = resultsForPage.Data[0];

                    Assert.Equal("Microsoft.DotNet.Common.ProjectTemplates.5.0", packageInfo.Name);
                    Assert.NotEmpty(packageInfo.Version);
                    Assert.True(packageInfo.TotalDownloads > 0);
                    Assert.True(packageInfo.Verified);
                    Assert.Contains("Microsoft", packageInfo.Owners);
                    Assert.NotNull(packageInfo.Description);
                    Assert.NotNull(packageInfo.IconUrl);
                    Assert.NotEmpty(packageInfo.Description);
                    Assert.NotEmpty(packageInfo.IconUrl);
                }
                else
                {
                    Assert.True(false, "HTTP request failed.");
                }
            }
        }
    }
}
