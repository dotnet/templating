using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Abstractions.TemplatesSources
{
    public interface ITemplatesSourcesProvider
    {

        Task<IReadOnlyList<ITemplatesSource>> GetAllSourcesAsync(CancellationToken cancellationToken);


        event Action SourcesChanged;


        ITemplatesSourcesProviderFactory Factory { get; }
    }
}
