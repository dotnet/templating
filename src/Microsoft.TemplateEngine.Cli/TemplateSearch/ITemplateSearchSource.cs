using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli.TemplateSearch
{
    public interface ITemplateSearchSource : IIdentifiedComponent
    {
        bool TryConfigure(IEngineEnvironmentSettings environmentSettings);

        Task<IReadOnlyList<ITemplateNameSearchResult>> CheckForTemplateNameMatchesAsync(string templateName);

        string DisplayName { get; }
    }
}
