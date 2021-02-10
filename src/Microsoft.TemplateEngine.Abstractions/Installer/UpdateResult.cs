// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.TemplateEngine.Abstractions.TemplatesSources;

namespace Microsoft.TemplateEngine.Abstractions.Installer
{
    public class UpdateResult : Result
    {
        public UpdateRequest UpdateRequest { get; private set; }

        public static UpdateResult FromInstallResult(UpdateRequest request, InstallResult installResult)
        {
            return new UpdateResult()
            {
                UpdateRequest = request,
                Source = installResult.Source,
                Error = installResult.Error,
                ErrorMessage = installResult.ErrorMessage
            };
        }
    }
}
