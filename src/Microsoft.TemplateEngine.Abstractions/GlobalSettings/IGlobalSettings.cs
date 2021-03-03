// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Abstractions.GlobalSettings
{
    public interface IGlobalSettings
    {
        event Action SettingsChanged;

        Task<IReadOnlyList<TemplatesSourceData>> GetInstalledTemplatesPackagesAsync(CancellationToken cancellationToken);

        Task SetInstalledTemplatesPackagesAsync(IReadOnlyList<TemplatesSourceData> packages, CancellationToken cancellationToken);

        Task<IDisposable> LockAsync(CancellationToken token);
    }
}
