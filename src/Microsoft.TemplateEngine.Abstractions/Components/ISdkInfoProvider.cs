// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Abstractions.Components
{
    /// <summary>
    /// Provider of SDK installation info.
    /// </summary>
    public interface ISdkInfoProvider : IIdentifiedComponent
    {
        /// <summary>
        /// Current SDK installation semver version string.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>SDK version.</returns>
        public Task<string> GetCurrentVersionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// All installed SDK installations semver version strings.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>SDK version strings.</returns>
        public Task<IEnumerable<string>> GetInstalledVersionsAsync(CancellationToken cancellationToken);
    }
}
