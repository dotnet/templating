// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.TemplateEngine.TemplateLocalizer.Core
{
    /// <summary>
    /// Creates and updates localization files for template.json files.
    /// Public members of this type is thread-safe.
    /// </summary>
    public sealed class TemplateLocalizer
    {
        private readonly ILogger _logger;

        public TemplateLocalizer() : this(null) { }

        public TemplateLocalizer(ILoggerFactory? loggerFactory)
        {
            _logger = (ILogger?)loggerFactory?.CreateLogger<TemplateLocalizer>() ?? NullLogger.Instance;
        }

        public async Task<ExportResult> ExportLocalizationFilesAsync(string templateJsonPath, ExportOptions options, CancellationToken cancellationToken = default)
        {
            using FileStream fileStream = new FileStream(templateJsonPath, FileMode.Open, FileAccess.Read);

            JsonDocumentOptions jsonOptions = new JsonDocumentOptions()
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };
            using JsonDocument jsonDocument = await JsonDocument.ParseAsync(fileStream, jsonOptions, cancellationToken).ConfigureAwait(false);

            TemplateStringExtractor stringExtractor = new TemplateStringExtractor(jsonDocument);
            var strings = stringExtractor.ExtractStrings();

            // This section is not implemented yet and will be delivered in the future commits. Please ignore during review.

            return new ExportResult(templateJsonPath, "Operation Failed", null);
        }
    }
}
