using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Abstractions.TemplatesSources
{
    public interface ITemplatesSourcesManager
    {
        /// <summary>
        /// Triggered every time when list of <see cref="ITemplatesSource"/> changes, this is triggered by <see cref="ITemplatesSourcesProvider.SourcesChanged"/>.
        /// </summary>
        event Action SourcesChanged;

        /// <summary>
        /// Returns combined list of <see cref="ITemplatesSource"/> that all <see cref="ITemplatesSourcesProvider"/>s and <see cref="IManagedTemplatesSourcesProvider"/>s return.
        /// </summary>
        /// <param name="force">Invalidates cache and queries all providers.</param>
        /// <returns></returns>
        Task<IReadOnlyList<ITemplatesSource>> GetTemplatesSources(bool force = false);

        /// <summary>
        /// This is same as <see cref="GetTemplatesSources"/> but filters only <see cref="IManagedTemplatesSource"/> types of sources.
        /// </summary>
        /// <param name="force">Invalidates cache and queries all providers.</param>
        /// <returns></returns>
        Task<IReadOnlyList<IManagedTemplatesSource>> GetManagedTemplatesSources(bool force = false);

        /// <summary>
        /// This is helper method for <see cref="GetManagedTemplatesSources"/> with <see cref="System.Linq.Enumerable.GroupBy"/>
        /// </summary>
        /// <param name="force">If true, invalidates cache</param>
        /// <returns></returns>
        Task<IReadOnlyList<(IManagedTemplatesSourcesProvider Provider, IReadOnlyList<IManagedTemplatesSource> ManagedSources)>> GetManagedSourcesGroupedByProvider(bool force = false);

        /// <summary>
        /// Returns <see cref="IManagedTemplatesSourcesProvider"/> with specified name
        /// </summary>
        /// <param name="name">Name from <see cref="ITemplatesSourcesProviderFactory.Name"/>.</param>
        /// <returns></returns>
        IManagedTemplatesSourcesProvider GetManagedProvider(string name);

        /// <summary>
        /// Returns <see cref="IManagedTemplatesSourcesProvider"/> with specified Guid
        /// </summary>
        /// <param name="id">Guid from <see cref="IIdentifiedComponent.Id"/> of <see cref="ITemplatesSourcesProviderFactory"/>.</param>
        /// <returns></returns>
        IManagedTemplatesSourcesProvider GetManagedProvider(Guid id);
    }
}
