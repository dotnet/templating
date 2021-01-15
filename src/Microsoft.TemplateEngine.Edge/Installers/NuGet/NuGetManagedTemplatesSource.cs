using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class NuGetManagedTemplatesSource : IManagedTemplatesSource
    {
        public NuGetManagedTemplatesSource(IManagedTemplatesSourcesProvider provider, string mountPoint, Dictionary<string, string> details)
        {
            if (!details.TryGetValue("PackageId", out var packageId))
                throw new ArgumentException("PackageId");
            if (!details.TryGetValue("Version", out var versionString) || !SemanticVersion.TryParse(versionString, out var version))
                throw new ArgumentException("Version");
            var nuGetSources = details.TryGetValue("NuGetSources", out var nugetSources) ? nugetSources.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) : null;

            ManagedProvider = provider;
            MountPointUri = mountPoint;
            Identifier = packageId;
            Version = version;
            Details = details;

        }

        public string Identifier { get; }

        public SemanticVersion Version { get; }

        public IReadOnlyDictionary<string, string> Details { get; }

        static List<string> detailKeysDisplayOrder = new List<string>() { "Author", "NuGetSource" };

        public IReadOnlyList<string> DetailKeysDisplayOrder => detailKeysDisplayOrder;

        public DateTime LastChangeTime { get; }

        public string MountPointUri { get; }

        public ITemplatesSourcesProvider Provider => ManagedProvider;

        public IManagedTemplatesSourcesProvider ManagedProvider { get; }
    }

}
