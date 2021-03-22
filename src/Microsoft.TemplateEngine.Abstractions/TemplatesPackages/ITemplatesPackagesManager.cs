using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Abstractions.TemplatesPackages
{
    public interface ITemplatesPackagesManager
    {
        /// <summary>
        /// Triggered every time when list of <see cref="ITemplatesPackage"/> changes, this is triggered by <see cref="ITemplatesPackagesProvider.SourcesChanged"/>.
        /// </summary>
        event Action SourcesChanged;

        /// <summary>
        /// Returns combined list of <see cref="ITemplatesPackage"/> that all <see cref="ITemplatesPackagesProvider"/>s and <see cref="IManagedTemplatesPackagesProvider"/>s return.
        /// </summary>
        /// <param name="force">Invalidates cache and queries all providers.</param>
        /// <returns></returns>
        Task<IReadOnlyList<ITemplatesPackage>> GetTemplatesPackages(bool force = false);

        /// <summary>
        /// This is same as <see cref="GetTemplatesPackages"/> but filters only <see cref="IManagedTemplatesPackage"/> types of sources.
        /// </summary>
        /// <param name="force">Invalidates cache and queries all providers.</param>
        /// <returns></returns>
        Task<IReadOnlyList<IManagedTemplatesPackage>> GetManagedTemplatesPackages(bool force = false);

        /// <summary>
        /// This is helper method for <see cref="GetManagedTemplatesPackages"/> with <see cref="System.Linq.Enumerable.GroupBy"/>
        /// </summary>
        /// <param name="force">If true, invalidates cache</param>
        /// <returns></returns>
        Task<IReadOnlyList<(IManagedTemplatesPackagesProvider Provider, IReadOnlyList<IManagedTemplatesPackage> ManagedSources)>> GetManagedSourcesGroupedByProvider(bool force = false);

        /// <summary>
        /// Returns <see cref="IManagedTemplatesPackagesProvider"/> with specified name
        /// </summary>
        /// <param name="name">Name from <see cref="ITemplatesPackagesProviderFactory.Name"/>.</param>
        /// <returns></returns>
        IManagedTemplatesPackagesProvider GetManagedProvider(string name);

        /// <summary>
        /// Returns <see cref="IManagedTemplatesPackagesProvider"/> with specified Guid
        /// </summary>
        /// <param name="id">Guid from <see cref="IIdentifiedComponent.Id"/> of <see cref="ITemplatesPackagesProviderFactory"/>.</param>
        /// <returns></returns>
        IManagedTemplatesPackagesProvider GetManagedProvider(Guid id);
    }
}
