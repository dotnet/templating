﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

namespace Microsoft.TemplateEngine.Cli.Commands
{
    internal static class ParserFactory
    {
        internal static Parser CreateTemplateParser(Command command)
        {
            return new CommandLineBuilder(command)
            .UseHelp()
            .UseParseErrorReporting()
            .UseParseDirective()
            .UseSuggestDirective()
            .DisablePosixBundling()
            .Build();
        }

        internal static Parser CreateParser(Command command)
        {
            return new CommandLineBuilder(command)
            .UseParseErrorReporting()
            .DisablePosixBundling()
            .Build();
        }

        private static CommandLineBuilder DisablePosixBundling(this CommandLineBuilder builder)
        {
            builder.EnablePosixBundling = false;
            return builder;
        }

    }
}
