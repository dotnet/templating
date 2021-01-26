using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class PackageNotFoundException : Exception
    {
        public string PackageIdentifier { get; private set; }
        public NuGetVersion PackageVersion { get; private set; }
        public IEnumerable<string> SourcesList { get; private set; }

        public PackageNotFoundException(string packageIdentifier, IEnumerable<string> attemptedSources) : base ($"{packageIdentifier} was not found in NuGet feeds {string.Join(";", attemptedSources)}")
        {
            PackageIdentifier = packageIdentifier;
            SourcesList = attemptedSources;
        }

        public PackageNotFoundException(string packageIdentifier, NuGetVersion packageVersion, IEnumerable<string> attemptedSources) : base($"{packageIdentifier}::{packageVersion} was not found in NuGet feeds {string.Join(";", attemptedSources)}")
        {
            PackageIdentifier = packageIdentifier;
            PackageVersion = packageVersion;
            SourcesList = attemptedSources;
        }
    }
}
