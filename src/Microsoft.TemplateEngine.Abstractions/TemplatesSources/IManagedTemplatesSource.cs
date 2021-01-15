using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Abstractions.TemplatesSources
{
    public interface IManagedTemplatesSource : ITemplatesSource
    {
        string Identifier { get; }

        SemanticVersion Version { get; }

        IReadOnlyDictionary<string, string> Details { get; }

        IReadOnlyList<string> DetailKeysDisplayOrder { get; }

        /// <summary>
        /// ManagedProvider that created this source.
        /// This serves as helper for grouping sources by provider
        /// so caller doesn't need to keep track of provider->source relation.
        /// </summary>
        IManagedTemplatesSourcesProvider ManagedProvider { get; }
    }
}
