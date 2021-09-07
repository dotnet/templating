﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Cli.Commands
{
    internal static class CommandFactory
    {
        internal static IEnumerable<Func<ITemplateEngineHost, ITelemetryLogger, New3Callbacks, IBaseCommand>> GetSubcommands()
        {
            yield return (host, telemetryLogger, callbacks) => new InstantiateCommand(host, telemetryLogger, callbacks);
            //yield return (host, telemetryLogger, callbacks) => new ListCommand(host, telemetryLogger, callbacks);
            //yield return (host, telemetryLogger, callbacks) => new SearchCommand(host, telemetryLogger, callbacks);
            yield return (host, telemetryLogger, callbacks) => new InstallCommand(host, telemetryLogger, callbacks);
            //yield return (host, telemetryLogger, callbacks) => new UninstallCommand(host, telemetryLogger, callbacks);
            //yield return (host, telemetryLogger, callbacks) => new UpdateCommand(host, telemetryLogger, callbacks);
            //yield return (host, telemetryLogger, callbacks) => new AddAliasCommand(host, telemetryLogger, callbacks);
            //yield return (host, telemetryLogger, callbacks) => new ShowAliasCommand(host, telemetryLogger, callbacks);
        }
    }
}
