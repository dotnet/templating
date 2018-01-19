using System;

namespace Microsoft.TemplateEngine.Cli
{
    // For setting limits on what gets reported.
    public enum ReporterMode
    {
        Json
    };

    internal class Reporter
    {
        private static readonly Reporter NullReporter = new Reporter(console: null);
        private static object _lock = new object();

        private readonly AnsiConsole _console;

        static Reporter()
        {
            Reset();
        }

        private Reporter(AnsiConsole console)
        {
            _console = console;
        }

        public static Reporter Output { get; private set; }
        public static Reporter Error { get; private set; }
        public static Reporter Verbose { get; private set; }

        // restricting output mode
        public static void SetMode(ReporterMode mode)
        {
            _mode = mode;
        }

        private static ReporterMode? _mode;

        /// <summary>
        /// Resets the Reporters to write to the current Console Out/Error.
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                Output = new Reporter(AnsiConsole.GetOutput());
                Error = new Reporter(AnsiConsole.GetError());
                Verbose = IsVerbose ?
                    new Reporter(AnsiConsole.GetOutput()) :
                    NullReporter;
                _mode = null;
            }
        }

        private bool ShouldWriteForMode(ReporterMode? forMode)
        {
            // The Output channel is the only restrictable channel.
            if (this != Output)
            {
                return true;
            }

            // reporter is set to unrestricted mode, the "forMode" is irrelevant
            if (_mode == null)
            {
                return true;
            }

            // the mode is set, the "ForMode" must match.
            return _mode == forMode;
        }

        public void WriteLine(string message)
        {
            WriteLine(message, null);
        }

        public void WriteLine(string message, ReporterMode? forMode)
        {
            lock (_lock)
            {
                if (ShouldWriteForMode(forMode))
                {
                    if (ShouldPassAnsiCodesThrough)
                    {
                        _console?.Writer?.WriteLine(message);
                    }
                    else
                    {
                        _console?.WriteLine(message);
                    }
                }
            }
        }

        public void WriteLine()
        {
            WriteLine((ReporterMode?)null);
        }

        public void WriteLine(ReporterMode? forMode)
        {
            lock (_lock)
            {
                if (ShouldWriteForMode(forMode))
                {
                    _console?.Writer?.WriteLine();
                }
            }
        }

        public void Write(string message)
        {
            Write(message, null);
        }

        public void Write(string message, ReporterMode? forMode)
        {
            lock (_lock)
            {
                if (ShouldWriteForMode(forMode))
                {
                    if (ShouldPassAnsiCodesThrough)
                    {
                        _console?.Writer?.Write(message);
                    }
                    else
                    {
                        _console?.Write(message);
                    }
                }
            }
        }

        private static bool IsVerbose
        {
            get { return bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_CLI_CONTEXT_VERBOSE") ?? "false", out bool value) && value; }
        }

        private bool ShouldPassAnsiCodesThrough
        {
            get { return bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_CLI_CONTEXT_ANSI_PASS_THRU") ?? "false", out bool value) && value; }
        }
    }
}
