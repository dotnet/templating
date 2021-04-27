﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        public Task<ExportResult> ExportLocalizationFilesAsync(string templateJsonPath, ExportOptions options, CancellationToken cancellationToken = default)
        {
            ExportResult result = new ExportResult();
            result.TemplateJsonPath = templateJsonPath;
            result.ErrorMessage = "Operation failed.";
            return Task.FromResult(result);
        }
    }
}
