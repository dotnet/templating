using NuGet.Versioning;
using System;
using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class DownloadException : Exception
    {
        public string PackageIdentifier { get; private set; }
        public NuGetVersion PackageVersion { get; private set; }
        public IEnumerable<string> SourcesList { get; private set; }

        public DownloadException(string packageIdentifier, NuGetVersion packageVersion, IEnumerable<string> attemptedSources) : base($"Failed to download {packageIdentifier}::{packageVersion} from NuGet feeds {string.Join(";", attemptedSources)}")
        {
            PackageIdentifier = packageIdentifier;
            PackageVersion = packageVersion;
            SourcesList = attemptedSources;
        }

        public DownloadException(string packageIdentifier, NuGetVersion packageVersion, IEnumerable<string> attemptedSources, Exception inner) : base($"Failed to download{packageIdentifier}::{packageVersion} from NuGet feeds {string.Join(";", attemptedSources)}", inner)
        {
            PackageIdentifier = packageIdentifier;
            PackageVersion = packageVersion;
            SourcesList = attemptedSources;
        }
    }
}
