// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.DotNet.Cli.Utils;
using Xunit.Abstractions;

namespace Microsoft.NET.TestFramework.Commands
{
    public abstract class TestCommand
    {
        protected TestCommand(ITestOutputHelper log)
        {
            Log = log;
        }

        public ITestOutputHelper Log { get; }

        public string? WorkingDirectory { get; set; }

        public List<string> Arguments { get; set; } = new List<string>();

        public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();

        public List<string> EnvironmentToRemove { get; } = new List<string>();

        public Encoding? StandardOutputEncoding { get; set; }

        public Encoding? StandardErrorEncoding { get; set; }

        /// <summary>
        /// Only triggers during the execution of <see cref="Execute()"/>
        /// and not during <see cref="GetProcessStartInfo(string[])"/>.
        /// </summary>
        public Action<string>? CommandOutputHandler { get; set; }

        /// <summary>
        /// Only triggers during the execution of <see cref="Execute()"/>
        /// and not during <see cref="GetProcessStartInfo(string[])"/>.
        /// </summary>
        public Action<Process>? ProcessStartedHandler { get; set; }

        public TestCommand WithEnvironmentVariable(string name, string value)
        {
            EnvironmentVariables[name] = value;
            return this;
        }

        public TestCommand WithWorkingDirectory(string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
            return this;
        }

        public TestCommand WithStandardOutputEncoding(Encoding encoding)
        {
            StandardOutputEncoding = encoding;
            return this;
        }

        public TestCommand WithStandardErrorEncoding(Encoding encoding)
        {
            StandardErrorEncoding = encoding;
            return this;
        }

        public ProcessStartInfo GetProcessStartInfo(params string[] args)
        {
            var commandSpec = CreateCommandSpec(args);

            var psi = commandSpec.ToProcessStartInfo();

            return psi;
        }

        public CommandResult Execute(params string[] args)
        {
            IEnumerable<string> enumerableArgs = args;
            return Execute(enumerableArgs);
        }

        public virtual CommandResult Execute(IEnumerable<string> args)
        {
            var command = CreateCommandSpec(args)
                .ToCommand()
                .CaptureStdOut()
                .CaptureStdErr();

            if (CommandOutputHandler != null)
            {
                command.OnOutputLine(CommandOutputHandler);
            }

            var result = ((Command)command).Execute(ProcessStartedHandler);

            Log.WriteLine($"> {result.StartInfo.FileName} {result.StartInfo.Arguments}");
            Log.WriteLine(result.StdOut);

            if (!string.IsNullOrEmpty(result.StdErr))
            {
                Log.WriteLine("");
                Log.WriteLine("StdErr:");
                Log.WriteLine(result.StdErr);
            }

            if (result.ExitCode != 0)
            {
                Log.WriteLine($"Exit Code: {result.ExitCode}");
            }

            return result;
        }

        protected abstract SdkCommandSpec CreateCommand(IEnumerable<string> args);

        private SdkCommandSpec CreateCommandSpec(IEnumerable<string> args)
        {
            var commandSpec = CreateCommand(args);
            foreach (var kvp in EnvironmentVariables)
            {
                commandSpec.Environment[kvp.Key] = kvp.Value;
            }

            foreach (var envToRemove in EnvironmentToRemove)
            {
                commandSpec.EnvironmentToRemove.Add(envToRemove);
            }

            if (WorkingDirectory != null)
            {
                commandSpec.WorkingDirectory = WorkingDirectory;
            }

            if (Arguments.Any())
            {
                commandSpec.Arguments = Arguments.Concat(commandSpec.Arguments).ToList();
            }

            commandSpec.StandardOutputEncodingOverride = StandardOutputEncoding ?? commandSpec.StandardOutputEncodingOverride;
            commandSpec.StandardErrorEncodingOverride = StandardErrorEncoding ?? commandSpec.StandardErrorEncodingOverride;

            return commandSpec;
        }
    }
}
