// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.DotNet.Cli.Utils
{ 
    public class Command
    {
        private readonly Process _process;

        private StreamForwarder _stdOut;

        private StreamForwarder _stdErr;

        private bool _running = false;

        private bool _trimTrailingNewlines = false;

        public Command(Process process, bool trimtrailingNewlines = false)
        {
            _trimTrailingNewlines = trimtrailingNewlines;
            _process = process ?? throw new ArgumentNullException(nameof(process));
        }

        public CommandResult Execute()
        {
            return Execute(_ => { });
        }
        public CommandResult Execute(Action<Process> processStarted)
        {
            Console.WriteLine(string.Format(
                "Running {0} {1}",
                _process.StartInfo.FileName,
                _process.StartInfo.Arguments));

            ThrowIfRunning();

            _running = true;

            _process.EnableRaisingEvents = true;

#if DEBUG
            var sw = Stopwatch.StartNew();

            Console.WriteLine($"> {FormatProcessInfo(_process.StartInfo)}");
#endif
            _process.Start();
            if (processStarted != null)
            {
                processStarted(_process);
            }

            Console.WriteLine(string.Format(
                "Process ID: {0}",
                _process.Id));

            var taskOut = _stdOut?.BeginRead(_process.StandardOutput);
            var taskErr = _stdErr?.BeginRead(_process.StandardError);
            _process.WaitForExit();

            taskOut?.Wait();
            taskErr?.Wait();

            var exitCode = _process.ExitCode;

#if DEBUG
            var message = string.Format(
                "&lt; {0} exited with {1} in {2} ms.",
                FormatProcessInfo(_process.StartInfo),
                exitCode,
                sw.ElapsedMilliseconds);
            if (exitCode == 0)
            {
                Console.WriteLine(message);
            }
            else
            {
                Console.WriteLine(message);
            }
#endif

            return new CommandResult(
                _process.StartInfo,
                exitCode,
                _stdOut?.CapturedOutput,
                _stdErr?.CapturedOutput);
        }

        public Command WorkingDirectory(string projectDirectory)
        {
            _process.StartInfo.WorkingDirectory = projectDirectory;
            return this;
        }

        public Command EnvironmentVariable(string name, string value)
        {
            _process.StartInfo.Environment[name] = value;
            return this;
        }

        public Command CaptureStdOut()
        {
            ThrowIfRunning();
            EnsureStdOut();
            _stdOut.Capture(_trimTrailingNewlines);
            return this;
        }

        public Command CaptureStdErr()
        {
            ThrowIfRunning();
            EnsureStdErr();
            _stdErr.Capture(_trimTrailingNewlines);
            return this;
        }

        public Command OnOutputLine(Action<string> handler)
        {
            ThrowIfRunning();
            EnsureStdOut();

            _stdOut.ForwardTo(writeLine: handler);
            return this;
        }

        public Command OnErrorLine(Action<string> handler)
        {
            ThrowIfRunning();
            EnsureStdErr();

            _stdErr.ForwardTo(writeLine: handler);
            return this;
        }

        public string CommandName => _process.StartInfo.FileName;

        public string CommandArgs => _process.StartInfo.Arguments;

        private string FormatProcessInfo(ProcessStartInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.Arguments))
            {
                return info.FileName;
            }

            return info.FileName + " " + info.Arguments;
        }

        private void EnsureStdOut()
        {
            _stdOut = _stdOut ?? new StreamForwarder();
            _process.StartInfo.RedirectStandardOutput = true;
        }

        private void EnsureStdErr()
        {
            _stdErr = _stdErr ?? new StreamForwarder();
            _process.StartInfo.RedirectStandardError = true;
        }

        private void ThrowIfRunning([CallerMemberName] string memberName = null)
        {
            if (_running)
            {
                throw new InvalidOperationException(string.Format(
                    "Unable to invoke {0} after the command has been run",
                    memberName));
            }
        }
    }
}
