using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Abstractions.TemplatesPackages
{
    public interface ITemplatesPackagesProvider
    {

        Task<IReadOnlyList<ITemplatesPackage>> GetAllSourcesAsync(CancellationToken cancellationToken);


        event Action SourcesChanged;


        ITemplatesPackagesProviderFactory Factory { get; }
    }
}
