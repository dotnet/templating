using System.IO;
using System.Net;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Edge;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource
{
    public class NuGetMetadataSearchSource : FileMetadataSearchSource
    {
        private static readonly string _templateDiscoveryMetadataFile = "nugetTemplateSearchInfo.json";

        private readonly ISearchInfoFileProvider _searchInfoFileProvider;

        public NuGetMetadataSearchSource()
        {
            _searchInfoFileProvider = new TEMP_LocalSourceFileProvider();
            //_searchInfoFileProvider = new BlobStoreSourceFileProvider();
        }

        public override bool TryConfigure(IEngineEnvironmentSettings environmentSettings)
        {
            if (!_searchInfoFileProvider.TryEnsureSearchFile(environmentSettings))
            {
                return false;
            }

            FileMetadataTemplateSearchCache searchCache = new FileMetadataTemplateSearchCache(environmentSettings, _templateDiscoveryMetadataFile);

            Configure(searchCache);

            return true;
        }

        public override string DisplayName => "NuGet";

        // Search data file acquisition:

        private interface ISearchInfoFileProvider
        {
            bool TryEnsureSearchFile(IEngineEnvironmentSettings environment);
        }

        private class BlobStoreSourceFileProvider : ISearchInfoFileProvider
        {
            // TODO: get an fwlink for this
            private static readonly string _searchMetadataUrl = "";

            public BlobStoreSourceFileProvider()
            {
            }

            public bool TryEnsureSearchFile(IEngineEnvironmentSettings environment)
            {
                Paths paths = new Paths(environment);
                string searchMetadataFileLocation = Path.Combine(paths.User.BaseDir, _templateDiscoveryMetadataFile);

                if (TryAcquireFileFromCloud(paths, searchMetadataFileLocation))
                {
                    return true;
                }

                if (paths.FileExists(searchMetadataFileLocation))
                {
                    // an old version of the file is already setup, fallback to using it.
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

        // TEMP - for testing until we get a real search file management story.
        private class TEMP_LocalSourceFileProvider : ISearchInfoFileProvider
        {
            private static readonly string _searchFileOriginalLocation = @"C:\Github\TemplateSearch\TempSearchResults\Unified\InitialData\SearchCache\templateSearchInfo.json";

            public TEMP_LocalSourceFileProvider()
            {
            }

            public bool TryEnsureSearchFile(IEngineEnvironmentSettings environment)
            {
                Paths paths = new Paths(environment);
                string targetPath = Path.Combine(paths.User.BaseDir, _templateDiscoveryMetadataFile);

                if (File.Exists(_searchFileOriginalLocation))
                {
                    // the original file exists, try to copy it to the config location.

                    paths.Copy(_searchFileOriginalLocation, targetPath);

                    return true;
                }

                if (paths.FileExists(targetPath))
                {
                    // an old version of the file is already setup, fallback to using it.
                    return true;
                }

                return false;
            }
        }
    }
}
