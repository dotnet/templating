// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.ConfigModel;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    /// <summary>
    /// Information available for template validation.
    /// </summary>
    internal interface ITemplateValidationInfo : IDisposable
    {
        /// <summary>
        /// Gets current environment settigs.
        /// </summary>
        IEngineEnvironmentSettings EngineEnvironmentSettings { get; }

        /// <summary>
        /// Gets logger.
        /// </summary>
        ILogger Logger { get; }

        /// <summary>
        /// Gets template configuration.
        /// </summary>
        TemplateConfigModel ConfigModel { get; }

        /// <summary>
        /// Gets source template directory.
        /// </summary>
        IDirectory TemplateSourceRoot { get; }

        /// <summary>
        /// Gets template configuration file.
        /// </summary>
        IFile? ConfigFile { get; }

        /// <summary>
        /// Gets localizations available for the template. Might not be available.
        /// </summary>
        IReadOnlyDictionary<string, ILocalizationLocator>? Localizations { get; }
    }
}
