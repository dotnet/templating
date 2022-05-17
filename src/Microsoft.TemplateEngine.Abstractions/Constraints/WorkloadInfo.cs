// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Abstractions.Constraints
{
    /// <summary>
    /// SDK workload descriptor.
    /// Analogous to SDK type Microsoft.NET.Sdk.WorkloadManifestReader.WorkloadResolver.WorkloadInfo.
    /// </summary>
    public class WorkloadInfo
    {
        /// <summary>
        /// Creates new instance of <see cref="WorkloadInfo"/>.
        /// </summary>
        /// <param name="id">Workload identifier.</param>
        /// <param name="description">Workload description string - expected to be localized.</param>
        public WorkloadInfo(string id, string description)
        {
            Id = id;
            Description = description;
        }

        /// <summary>
        /// Workload identifier (from manifest).
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Workload description string - expected to be localized.
        /// </summary>
        public string Description { get; }
    }
}
