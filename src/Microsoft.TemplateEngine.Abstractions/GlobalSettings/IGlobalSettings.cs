// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Abstractions.GlobalSettings
{
    /// <summary>
    /// Interface that represents loading/storing data into settings.json file.
    /// That is shared between multiple different hosts of TemplateEngine.
    /// </summary>
    public interface IGlobalSettings
    {
        /// <summary>
        /// Triggered every time when settigns change.
        /// </summary>
        event Action SettingsChanged;

        /// <summary>
        /// Returns uncached list of installer template packages.
        /// </summary>
        Task<IReadOnlyList<TemplatesSourceData>> GetInstalledTemplatesPackagesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stores list of installer template packages.
        /// </summary>
        Task SetInstalledTemplatesPackagesAsync(IReadOnlyList<TemplatesSourceData> packages, CancellationToken cancellationToken);

        /// <summary>
        /// This method must be called before making any modifications to settings to ensure other processes on system
        /// don't override or lose changes done by this process.
        /// </summary>
        /// <returns><see cref="IDisposable"/> object that needs to be disposed once modifying of settings is finished.</returns>
        Task<IDisposable> LockAsync(CancellationToken token);
    }
}
