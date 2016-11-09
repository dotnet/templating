﻿using System;

namespace Microsoft.TemplateEngine.Abstractions
{
    public interface ILocalizationLocator
    {
        string Locale { get; }

        Guid MountPointId { get; }

        string ConfigPlace { get; }

        string Identity { get; }
    }
}
