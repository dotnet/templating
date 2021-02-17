using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatesSources;

namespace Microsoft.TemplateSearch.Common
{
    public interface ITemplateSearchSource : IIdentifiedComponent
    {
        Task<bool> TryConfigure(IEngineEnvironmentSettings environmentSettings, IReadOnlyList<IManagedTemplatesSource> existingTemplatesSource);

        Task<IReadOnlyList<ITemplateNameSearchResult>> CheckForTemplateNameMatchesAsync(string templateName);

        Task<IReadOnlyDictionary<string, PackToTemplateEntry>> CheckForTemplatePackMatchesAsync(IReadOnlyList<string> packNameList);

        string DisplayName { get; }
    }
}
