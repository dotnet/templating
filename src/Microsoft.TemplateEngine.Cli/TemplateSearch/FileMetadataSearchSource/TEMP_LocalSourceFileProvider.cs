using System.IO;
using Microsoft.TemplateEngine.Edge;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch.FileMetadataSearchSource
{
    // TEMP - for testing until we get a real search file management story.
    internal class TEMP_LocalSourceFileProvider : ISearchInfoFileProvider
    {
        private static readonly string _searchFileOriginalLocation = @"C:\Github\TemplateSearch\TempSearchResults\WithGroup\SearchCache\templateSearchInfo.json";

        public TEMP_LocalSourceFileProvider()
        {
        }

        public bool TryEnsureSearchFile(Paths paths, string metadataFileTargetLocation)
        {
            if (File.Exists(_searchFileOriginalLocation))
            {
                // the original file exists, try to copy it to the config location.
                paths.Copy(_searchFileOriginalLocation, metadataFileTargetLocation);

                return true;
            }

            // an old version of the file may already be setup. If so, fallback to using it.
            return paths.FileExists(metadataFileTargetLocation);
        }
    }
}
