// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Abstractions.GlobalSettings
{
    public interface IGlobalSettings
    {
        event Action SettingsChanged;

        string DefaultLanguage { get; set; }
        IReadOnlyList<TemplatesSourceData> UserInstalledTemplatesSources { get; }

        void Add(TemplatesSourceData userInstalledTemplate);

        void Remove(TemplatesSourceData userInstalledTemplate);
    }
}
