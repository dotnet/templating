// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Abstractions.Constraints
{
    /// <summary>
    /// Provider of descriptors of SDK workloads available to particular host (that is usually providing this component).
    /// </summary>
    public interface IWorkloadsInfoProvider : IIdentifiedComponent
    {
        /// <summary>
        /// Set of installed workloads.
        /// </summary>
        public IEnumerable<WorkloadInfo> InstalledWorkloads { get; }
    }
}
