using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.TemplateSearch.TemplateDiscovery.PackProviders;
using Newtonsoft.Json;

namespace Microsoft.TemplateSearch.TemplateDiscovery.Nuget
{
    public class NugetPackProvider : IPackProvider
    {
        private static readonly string SearchUrlFormat = "https://api-v2v3search-0.nuget.org/query?q=template&skip={0}&take={1}&prerelease=true";

        private readonly string _packageTempPath;
        private readonly int _pageSize;
        private readonly bool _runOnlyOnePage;

        private static readonly string DownloadedPacksDir = "DownloadedPacks";

        public NugetPackProvider(string packageTempBasePath, int pageSize, bool runOnlyOnePage)
        {
            _pageSize = pageSize;
            _runOnlyOnePage = runOnlyOnePage;
            _packageTempPath = Path.Combine(packageTempBasePath, DownloadedPacksDir);

            if (Directory.Exists(_packageTempPath))
            {
                throw new Exception($"temp storage path for nuget packages already exists");
            }
            else
            {
                Directory.CreateDirectory(_packageTempPath);
            }
        }

        public IEnumerable<IPackInfo> CandidatePacks
        {
            get
            {
                int skip = 0;
                bool done = false;
                int packCount = 0;

                do
                {
                    string queryString = string.Format(SearchUrlFormat, skip, _pageSize);
                    WebRequest searchRequest = WebRequest.Create(queryString);
                    searchRequest.Method = "GET";
                    WebResponse response = searchRequest.GetResponseAsync().Result;
                    Stream responseStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(responseStream);
                    string searchResultText = reader.ReadToEnd();

                    NugetPackageSearchResult resultsForPage = JsonConvert.DeserializeObject<NugetPackageSearchResult>(searchResultText);

                    if (resultsForPage.Data.Count > 0)
                    {
                        skip += _pageSize;
                        packCount += resultsForPage.Data.Count;

                        foreach (NugetPackageSourceInfo sourceInfo in resultsForPage.Data)
                        {
                            if (TryDownloadPackage(sourceInfo, out string packageFilePath))
                            {
                                NugetPackInfo packInfo = new NugetPackInfo()
                                {
                                    VersionedPackageIdentity = sourceInfo.VersionedPackageIdentity,
                                    Id = sourceInfo.Id,
                                    Version = sourceInfo.Version,
                                    Path = packageFilePath
                                };

                                yield return packInfo;
                            }
                        }
                    }
                    else
                    {
                        done = true;
                    }
                } while (!done && !_runOnlyOnePage);
            }
        }

        private static readonly string DownloadUrlFormat = "https://api.nuget.org/v3-flatcontainer/{0}/{1}/{0}.{1}.nupkg";
        // {PackageId}.{Version}.nupkg
        private static readonly string DownloadPackageFileNameFormat = "{0}.{1}.nupkg";

        private bool TryDownloadPackage(NugetPackageSourceInfo packinfo, out string packageFilePath)
        {
            string downloadUrl = string.Format(DownloadUrlFormat, packinfo.Id, packinfo.Version);
            string packageFileName = string.Format(DownloadPackageFileNameFormat, packinfo.Id, packinfo.Version);
            string outputPackageFileNameFullPath = Path.Combine(_packageTempPath, packageFileName);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    byte[] packageBytes = client.GetByteArrayAsync(downloadUrl).Result;
                    File.WriteAllBytes(outputPackageFileNameFullPath, packageBytes);
                }
            }
            catch //(Exception ex)
            {
                packageFilePath = null;
                return false;
            }

            packageFilePath = outputPackageFileNameFullPath;
            return true;
        }
    }
}
