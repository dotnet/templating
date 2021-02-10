// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Abstractions.TemplatesSources;

namespace Microsoft.TemplateEngine.Abstractions.Installer
{
    public abstract class Result
    {
        public InstallerErrorCode Error { get; protected set; }

        public string ErrorMessage { get; protected set; }

        public IManagedTemplatesSource Source { get; protected set; }

        public bool Success => Error == 0;
    }
}
