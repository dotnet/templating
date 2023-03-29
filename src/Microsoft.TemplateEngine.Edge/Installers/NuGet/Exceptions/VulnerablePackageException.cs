// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using NuGet.Protocol;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class VulnerablePackageException : Exception
    {
        public VulnerablePackageException(string message, IEnumerable<PackageVulnerabilityMetadata> vulnerabilities)
            : base(message)
        {
            Vulnerabilities = vulnerabilities;
        }

        public IEnumerable<PackageVulnerabilityMetadata> Vulnerabilities { get; private set; }

        private string NiceTableFormat()
        {
            return "Some nice table format to list vulnerabilities";
        }
    }
}
