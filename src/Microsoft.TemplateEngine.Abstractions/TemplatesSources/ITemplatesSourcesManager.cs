using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Abstractions.TemplatesSources
{
    public interface ITemplatesSourcesManager
    {
        event Action SourcesChanged;

        Task<IReadOnlyList<ITemplatesSource>> GetTemplatesSources(bool force = false);

        Task<IReadOnlyList<IManagedTemplatesSource>> GetManagedTemplatesSources(bool force = false);

        /// <summary>
        /// This is helper method for <see cref="GetManagedTemplatesSources"/> with <see cref="System.Linq.Enumerable.GroupBy"/>
        /// </summary>
        /// <param name="force">If true, invalidates cache</param>
        /// <returns></returns>
        Task<IReadOnlyList<(IManagedTemplatesSourcesProvider Provider, IReadOnlyList<IManagedTemplatesSource> ManagedSources)>> GetManagedSourcesGroupedByProvider(bool force = false);
        IManagedTemplatesSourcesProvider GetManagedProvider(string name);
        IManagedTemplatesSourcesProvider GetManagedProvider(Guid id);
    }
}
