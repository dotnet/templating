// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Utils;
using Xunit.Abstractions;

namespace Microsoft.TemplateEngine.CommandUtils
{
    internal abstract class TestCommand
    {
        private readonly LoggerWrapper _loggerWrapper;

        protected TestCommand(ITestOutputHelper log)
        {
            _loggerWrapper = new LoggerWrapper(log);
        }

        protected TestCommand(ILogger log)
        {
            _loggerWrapper = new LoggerWrapper(log);
        }

        internal string? WorkingDirectory { get; set; }

        internal List<string> Arguments { get; set; } = new List<string>();

        internal List<string> EnvironmentToRemove { get; } = new List<string>();

        //  These only work via Execute(), not when using GetProcessStartInfo()
        internal Action<string>? CommandOutputHandler { get; set; }

        internal Action<Process>? ProcessStartedHandler { get; set; }

        protected Dictionary<string, string> Environment { get; set; } = new Dictionary<string, string>();

        internal TestCommand WithEnvironmentVariable(string name, string value)
        {
            Environment[name] = value;
            return this;
        }

        internal TestCommand WithEnvironmentVariables(IReadOnlyDictionary<string, string>? variables)
        {
            if (variables != null)
            {
                Environment.Merge(variables);
            }
            return this;
        }

        internal TestCommand WithWorkingDirectory(string workingDirectory)
        {
            WorkingDirectory = workingDirectory;
            return this;
        }

        internal TestCommand WithNoUpdateCheck()
        {
            Arguments.Add("--no-update-check");
            return this;
        }

        internal ProcessStartInfo GetProcessStartInfo(params string[] args)
        {
            SdkCommandSpec commandSpec = CreateCommandSpec(args);

            var psi = commandSpec.ToProcessStartInfo();

            return psi;
        }

        internal CommandResult Execute(params string[] args)
        {
            IEnumerable<string> enumerableArgs = args;
            return Execute(enumerableArgs);
        }

        internal virtual CommandResult Execute(IEnumerable<string> args)
        {
            Command command = CreateCommandSpec(args)
                .ToCommand()
                .CaptureStdOut()
                .CaptureStdErr();

            if (CommandOutputHandler != null)
            {
                command.OnOutputLine(CommandOutputHandler);
            }

            var result = command.Execute(ProcessStartedHandler);

            _loggerWrapper.WriteLine($"> {result.StartInfo.FileName} {result.StartInfo.Arguments}");
            _loggerWrapper.WriteLine(result.StdOut);

            if (!string.IsNullOrEmpty(result.StdErr))
            {
                _loggerWrapper.WriteLine(string.Empty);
                _loggerWrapper.WriteLine("StdErr:");
                _loggerWrapper.WriteLine(result.StdErr);
            }

            if (result.ExitCode != 0)
            {
                _loggerWrapper.WriteLine($"Exit Code: {result.ExitCode}");
            }

            return result;
        }

        private protected abstract SdkCommandSpec CreateCommand(IEnumerable<string> args);

        private SdkCommandSpec CreateCommandSpec(IEnumerable<string> args)
        {
            var commandSpec = CreateCommand(args);
            foreach (var kvp in Environment)
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

            return commandSpec;
        }

        private class LoggerWrapper
        {
            private readonly ILogger? _logger;
            private readonly ITestOutputHelper? _testHelper;

            internal LoggerWrapper(ILogger logger)
            {
                _logger = logger;
            }

            internal LoggerWrapper(ITestOutputHelper logger)
            {
                _testHelper = logger;
            }

            internal void WriteLine(string? message)
            {
                if (message is null)
                {
                    return;
                }
                _logger?.Log(LogLevel.Information, message);
                _testHelper?.WriteLine(message);
            }
        }
    }
}
