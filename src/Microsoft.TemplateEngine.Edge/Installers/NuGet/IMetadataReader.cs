// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal interface IMetadataReader
    {
        /// <summary>
        /// Returns owner and reserved property values for the template package based on id and source feed.
        /// </summary>
        /// <param name="packageIdentifier">Package Id.</param>
        /// <param name="source">Package source feed.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The package metadata required for package migration.</returns>
        Task<(string Owners, bool Verified)> GetMigrationPackageMetadata(
           string packageIdentifier,
           PackageSource source,
           CancellationToken cancellationToken = default);
    }
}
