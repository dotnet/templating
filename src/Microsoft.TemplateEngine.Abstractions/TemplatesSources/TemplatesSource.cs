using System;

namespace Microsoft.TemplateEngine.Abstractions.TemplatesSources
{
    /// <summary>
    /// This is basic <see cref="ITemplatesSource"/> implementation so each
    /// <see cref="ITemplatesSourcesProvider"/> doesn't need to re-implement.
    /// </summary>
    public class TemplatesSource : ITemplatesSource
    {
        public TemplatesSource(ITemplatesSourcesProvider provider, string mountPointUri, DateTime lastChangeTime)
        {
            Provider = provider;
            MountPointUri = mountPointUri;
            LastChangeTime = lastChangeTime;
        }

        public ITemplatesSourcesProvider Provider { get; }

        public string MountPointUri { get; }

        public DateTime LastChangeTime { get; }
    }
}
