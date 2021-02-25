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

        IReadOnlyList<TemplatesSourceData> UserInstalledTemplatesSources { get; }

        void Add(TemplatesSourceData userInstalledTemplate);

        void Remove(TemplatesSourceData userInstalledTemplate);

        Task LockAsync(CancellationToken token);

        Task UnlockAsync(CancellationToken token);
    }
}
