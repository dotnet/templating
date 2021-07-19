// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine;
using Microsoft.TemplateSearch.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateSearch.TemplateDiscovery.NuGet
{
    internal class NuGetPackageSearchResult
    {
        internal int TotalHits { get; private set; }

        internal List<PackInfo> Data { get; private set; } = new List<PackInfo>();

        internal static NuGetPackageSearchResult FromJObject(JObject entry)
        {
            NuGetPackageSearchResult searchResult = new NuGetPackageSearchResult();
            searchResult.TotalHits = entry.ToInt32(nameof(TotalHits));
            var dataArray = entry.Get<JArray>(nameof(Data));
            if (dataArray != null)
            {
                foreach (JToken data in dataArray)
                {
                    JObject? dataObj = data as JObject;
                    if (dataObj != null)
                    {
                        searchResult.Data.Add(PackInfoFromJObject(dataObj));
                    }
                }

            }
            return searchResult;
        }

        private static PackInfo PackInfoFromJObject(JObject dataObject)
        {
            const string idPropertyName = "id";

            string name = dataObject.ToString(idPropertyName)
                ?? throw new ArgumentException($"{nameof(dataObject)} doesn't have {nameof(idPropertyName)} property.", nameof(dataObject));
            string version = dataObject.ToString(nameof(PackInfo.Version))
                ?? throw new ArgumentException($"{nameof(dataObject)} doesn't have {nameof(PackInfo.Version)} property.", nameof(dataObject));

            int totalDownloads = dataObject.ToInt32(nameof(PackInfo.TotalDownloads));
            bool verified = dataObject.ToBool(nameof(PackInfo.Verified));

            IReadOnlyList<string> owners = dataObject.Get<JObject>(nameof(PackInfo.Owners)).JTokenStringOrArrayToCollection(Array.Empty<string>());

            return new PackInfo(name, version, totalDownloads, owners, verified);
        }
    }
}
