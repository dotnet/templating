﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;

namespace Microsoft.TemplateEngine.Authoring.CLI.Commands
{
    internal class LocalizeCommand : Command
    {
        internal LocalizeCommand()
            : base("localize")
        {
            this.Subcommands.Add(new ExportCommand());
        }
    }
}
