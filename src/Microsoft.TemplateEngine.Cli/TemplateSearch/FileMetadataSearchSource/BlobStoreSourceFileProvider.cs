using System.IO;
using System.Net;
using Microsoft.TemplateEngine.Edge;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource
{
    internal class BlobStoreSourceFileProvider : ISearchInfoFileProvider
    {
        private static readonly string _searchMetadataUrl = "https://go.microsoft.com/fwlink/?linkid=2087906&clcid=0x409";

        public BlobStoreSourceFileProvider()
        {
        }

        public bool TryEnsureSearchFile(Paths paths, string metadataFileTargetLocation)
        {
            if (TryAcquireFileFromCloud(paths, metadataFileTargetLocation))
            {
                return true;
            }

            // A previously acquired file may already be setup.
            // If could either be from online storage, or shipped in-box.
            // If so, fallback to using it.
            if (paths.FileExists(metadataFileTargetLocation))
            {
                return true;
            }

            // use the in-box shipped file. It's probably very stale, but better than nothing.
            if (paths.FileExists(paths.User.NuGetScrapedTemplateSearchFile))
            {
                paths.Copy(paths.User.NuGetScrapedTemplateSearchFile, metadataFileTargetLocation);
                return true;
            }

            return false;
        }

        private bool TryAcquireFileFromCloud(Paths paths, string searchMetadataFileLocation)
        {
            try
            {
                WebRequest request = WebRequest.Create(_searchMetadataUrl);
                request.Method = "GET";
                WebResponse response = request.GetResponseAsync().Result;
                Stream responseStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                string searchResultText = reader.ReadToEnd();

                paths.WriteAllText(searchMetadataFileLocation, searchResultText);

                return true;
            }
            catch
            {
                // TODO: consider additional action - alerting the user, etc.
                return false;
            }
        }
    }
}
