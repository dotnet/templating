// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.TemplateEngine.TestHelper
{
    public class LoggerHelper
    {
        private SharedTestOutputHelper _testOutputHelper;

        public LoggerHelper(IMessageSink messageSink)
        {
            _testOutputHelper = new SharedTestOutputHelper(messageSink);
        }

        public ILogger CreateLogger(IEnumerable<ILoggerProvider>? addLoggerProviders = null)
        {
            IEnumerable<ILoggerProvider> loggerProviders = new[] { new XunitLoggerProvider(_testOutputHelper) };
            if (addLoggerProviders != null)
            {
                loggerProviders = loggerProviders.Concat(addLoggerProviders);
            }

            var loggerFactory =
                Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                {
                    builder
                        .SetMinimumLevel(LogLevel.Trace);

                    if (addLoggerProviders?.Any() ?? false)
                    {
                        foreach (ILoggerProvider loggerProvider in addLoggerProviders)
                        {
                            builder.AddProvider(loggerProvider);
                        }
                    }
                    builder.AddSimpleConsole(options =>
                    {
                        options.SingleLine = true;
                        options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] ";
                        options.IncludeScopes = true;
                    });
                });
            return loggerFactory.CreateLogger("Test Host");
        }
    }
}
