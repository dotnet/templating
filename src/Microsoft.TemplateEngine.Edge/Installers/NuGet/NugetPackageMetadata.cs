// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace Microsoft.TemplateEngine.Edge.Installers.NuGet
{
    internal class NugetPackageMetadata
    {
        public NugetPackageMetadata(IPackageSearchMetadata metadata, string owners)
        {
            Authors = metadata.Authors;
            Identity = metadata.Identity;
            PrefixReserved = metadata.PrefixReserved;

            Owners = owners;
        }

        public string Authors { get; }

        public PackageIdentity Identity { get; }

        public string Owners { get; }

        public bool PrefixReserved { get; }
    }
}
