using System;

namespace Microsoft.TemplateEngine.Abstractions.TemplatesSources
{
    /// <summary>
    /// Templates source is folder, .nupkg or other container that can contain single or multiple templates.
    /// <seealso cref="ITemplatesSourcesProvider"/> for more information.
    /// </summary>
    public interface ITemplatesSource
    {
        /// <summary>
        /// To avoid scanning for changes every time. TemplateEngine is caching templates from
        /// template source, this timestamp is used to invalidate content and re-scan this templates source.
        /// </summary>
        DateTime LastChangeTime { get; }

        /// <summary>
        /// This can be file:// or simply
        /// </summary>
        string MountPointUri { get; }

        /// <summary>
        /// This is provider that created this source.
        /// This is mostly helper for grouping sources by provider
        /// so caller doesn't need to keep track of provider->source relation.
        /// </summary>
        ITemplatesSourcesProvider Provider { get; }
    }
}
