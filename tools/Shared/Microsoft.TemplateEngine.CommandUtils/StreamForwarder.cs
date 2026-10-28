// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.TemplateEngine.CommandUtils
{
    internal sealed class StreamForwarder
    {
        private const char FlushBuilderCharacter = '\n';
        private const char CarriageReturn = '\r';

        private StringBuilder? _builder;
        private StringWriter? _capture;
        private Action<string>? _writeLine;
        private bool _trimTrailingCapturedNewline;

        public string? CapturedOutput
        {
            get
            {
                string? capture = _capture?.GetStringBuilder()?.ToString();
                if (_trimTrailingCapturedNewline)
                {
                    capture = capture?.TrimEnd('\r', '\n');
                }
                return capture;
            }
        }

        public StreamForwarder Capture(bool trimTrailingNewline = false)
        {
            ThrowIfCaptureSet();

            _capture = new StringWriter();
            _trimTrailingCapturedNewline = trimTrailingNewline;

            return this;
        }

        public StreamForwarder ForwardTo(Action<string> writeLine)
        {
            ThrowIfNull(writeLine);

            ThrowIfForwarderSet();

            _writeLine = writeLine;

            return this;
        }

        // Use a dedicated long-running thread rather than a thread-pool thread (Task.Run).
        // Under load (e.g. CI running many tests in parallel) the thread pool can be
        // starved, delaying the reader from draining the child's redirected stream. When
        // that happens the OS pipe buffer fills, the child blocks on Console.Write, and an
        // asynchronous console logger (AddSimpleConsole) can drop queued messages when its
        // bounded flush-on-dispose (~1.5s) times out - producing truncated captured output.
        public Task BeginRead(TextReader reader) =>
            Task.Factory.StartNew(
                () => Read(reader),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

        public void Read(TextReader reader)
        {
            // Read in reasonably sized chunks so the pipe is drained quickly and the child
            // process is never blocked writing to a full pipe for long.
            const int bufferSize = 4096;
            char currentCharacter;

            char[] buffer = new char[bufferSize];
            _builder = new StringBuilder();

            int read;
            while ((read = reader.Read(buffer, 0, bufferSize)) > 0)
            {
                for (int i = 0; i < read; i++)
                {
                    currentCharacter = buffer[i];

                    if (currentCharacter == FlushBuilderCharacter)
                    {
                        WriteBuilder();
                    }
                    else if (currentCharacter != CarriageReturn)
                    {
                        _ = _builder.Append(currentCharacter);
                    }
                }
            }

            // Flush anything else when the stream is closed
            // Which should only happen if someone used console.Write
            if (_builder.Length > 0)
            {
                WriteBuilder();
            }
        }

        private void WriteBuilder()
        {
            WriteLine(_builder?.ToString());
            _ = (_builder?.Clear());
        }

        private void WriteLine(string? str)
        {
            _capture?.WriteLine(str);

            if (_writeLine != null && str != null)
            {
                _writeLine(str);
            }
        }

        private void ThrowIfNull(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
        }

        private void ThrowIfForwarderSet()
        {
            if (_writeLine != null)
            {
                throw new InvalidOperationException("WriteLine forwarder set previously");
            }
        }

        private void ThrowIfCaptureSet()
        {
            if (_capture != null)
            {
                throw new InvalidOperationException("Already capturing stream!");
            }
        }
    }
}
