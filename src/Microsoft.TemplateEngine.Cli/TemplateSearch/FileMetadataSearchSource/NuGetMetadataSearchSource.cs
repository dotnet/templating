using System.IO;
using System.Threading.Tasks;
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
            _searchInfoFileProvider = new BlobStoreSourceFileProvider();
        }

        public async override Task<bool> TryConfigureAsync(IEngineEnvironmentSettings environmentSettings)
        {
            Paths paths = new Paths(environmentSettings);
            string searchMetadataFileLocation = Path.Combine(paths.User.BaseDir, _templateDiscoveryMetadataFile);

            if (!await _searchInfoFileProvider.TryEnsureSearchFileAsync(paths, searchMetadataFileLocation))
            {
                return false;
            }

            FileMetadataTemplateSearchCache searchCache = new FileMetadataTemplateSearchCache(environmentSettings, _templateDiscoveryMetadataFile);

            Configure(searchCache);

            return true;
        }

        public override string DisplayName => "NuGet";
    }
}
