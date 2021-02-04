// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.TemplateEngine.Abstractions.TemplatesSources
{
    public interface IManagedTemplatesSourceUpdate
    {
        /// <summary>
        ///  The existing install unit that this update descriptor is for.
        /// </summary>
        IManagedTemplatesSource InstallUnitDescriptor { get; }

        /// <summary>
        /// Version which this update should install
        /// </summary>
        string Version { get; }
    }
}
