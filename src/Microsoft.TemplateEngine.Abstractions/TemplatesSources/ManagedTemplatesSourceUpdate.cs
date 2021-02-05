// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TemplateEngine.Abstractions.TemplatesSources
{
    public class ManagedTemplatesSourceUpdate
    {
        public ManagedTemplatesSourceUpdate(IManagedTemplatesSource source, string version)
        {
            InstallUnitDescriptor = source;
            Version = version;
        }

        /// <summary>
        ///  The existing install unit that this update descriptor is for.
        /// </summary>
        public IManagedTemplatesSource InstallUnitDescriptor { get; }

        /// <summary>
        /// Version which this update should install
        /// </summary>
        public string Version { get; }
    }
}
