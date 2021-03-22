using System;

namespace Microsoft.TemplateEngine.Abstractions.TemplatesPackages
{
    /// <summary>
    /// This is basic <see cref="ITemplatesPackage"/> implementation so each
    /// <see cref="ITemplatesPackagesProvider"/> doesn't need to re-implement.
    /// </summary>
    public class TemplatesPackage : ITemplatesPackage
    {
        public TemplatesPackage(ITemplatesPackagesProvider provider, string mountPointUri, DateTime lastChangeTime)
        {
            Provider = provider;
            MountPointUri = mountPointUri;
            LastChangeTime = lastChangeTime;
        }

        public ITemplatesPackagesProvider Provider { get; }

        public string MountPointUri { get; }

        public DateTime LastChangeTime { get; }
    }
}
