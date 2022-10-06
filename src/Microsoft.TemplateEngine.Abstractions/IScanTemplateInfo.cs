// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions
{
    /// <summary>
    /// Template information, used to be stored in the template cache.
    /// This information is common for all templates that can be managed by different <see cref="IGenerator"/>s.
    /// </summary>
    public interface IScanTemplateInfo : ITemplateMetadata, ITemplateLocator, IValidationInfo
    {
        /// <summary>
        /// Gets all localizations available for the template. The key is locale string.
        /// </summary>
        IReadOnlyDictionary<string, ILocalizationLocator> Localizations { get; }

        /// <summary>
        /// Gets all host files available for the template. The key is host identifier, the value is a relative path to the host file.
        /// </summary>
        IReadOnlyDictionary<string, string> HostConfigFiles { get; }
    }
}
