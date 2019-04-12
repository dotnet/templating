using System.IO;
using System.Net;
using Microsoft.TemplateEngine.Edge;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource
{
    internal class BlobStoreSourceFileProvider : ISearchInfoFileProvider
    {
        // TODO: get an fwlink for this
        private static readonly string _searchMetadataUrl = "";

        public BlobStoreSourceFileProvider()
        {
        }

        public bool TryEnsureSearchFile(Paths paths, string metadataFileTargetLocation)
        {
            if (TryAcquireFileFromCloud(paths, metadataFileTargetLocation))
            {
                return true;
            }

            // an old version of the file may already be setup. If so, fallback to using it.
            return paths.FileExists(metadataFileTargetLocation);
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
