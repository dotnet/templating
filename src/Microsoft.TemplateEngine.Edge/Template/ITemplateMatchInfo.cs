using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Edge.Template
{
    // Replacement for IFilteredTemplateInfo
    public interface ITemplateMatchInfo
    {
        ITemplateInfo Info { get; }

        IReadOnlyList<MatchInfo> MatchDisposition { get; }

        IReadOnlyList<MatchInfo> DispositionOfDefaults { get; }

        void AddDisposition(MatchInfo newDisposition);

        bool IsMatch { get; }

        bool IsPartialMatch { get; }
    }
}
