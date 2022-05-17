// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Abstractions.Constraints
{
    /// <summary>
    /// Provider of SDK installation info.
    /// </summary>
    public interface ISdkInfoProvider : IIdentifiedComponent
    {
        /// <summary>
        /// Current SDK installation semver version string.
        /// </summary>
        public string VersionString { get; }
    }
}
