// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions.Constraints;

namespace Microsoft.TemplateEngine.Abstractions.Components
{
    /// <summary>
    /// Provider of descriptors of SDK workloads available to particular host (that is usually providing this component).
    /// </summary>
    public interface IWorkloadsInfoProvider : IIdentifiedComponent
    {
        /// <summary>
        /// Fetches set of installed workloads.
        /// </summary>
        /// <param name="token"></param>
        /// <returns>Set of installed workloads.</returns>
        public Task<IEnumerable<WorkloadInfo>> GetInstalledWorkloadsAsync(CancellationToken token);
    }
}
