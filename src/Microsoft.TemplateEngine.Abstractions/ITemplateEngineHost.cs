// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;

namespace Microsoft.TemplateEngine.Abstractions
{
    public interface ITemplateEngineHost
    {
        IReadOnlyList<KeyValuePair<Guid, Func<Type>>> BuiltInComponents { get; }

        IPhysicalFileSystem FileSystem { get; }

        string HostIdentifier { get; }

        IReadOnlyList<string> FallbackHostTemplateConfigNames { get; }

        /// <summary>
        /// Gets default logger for given template engine host.
        /// </summary>
        ILogger Logger { get; }

        /// <summary>
        /// Gets logger factory for given template engine host.
        /// </summary>
        ILoggerFactory LoggerFactory { get; }

        string Version { get; }

        [Obsolete("Use " + nameof(Logger) + " instead.")]
        void LogTiming(string label, TimeSpan duration, int depth);

        [Obsolete("Use " + nameof(Logger) + " instead.")]
        void LogMessage(string message);

        [Obsolete("Use " + nameof(Logger) + " instead.")]
        void OnCriticalError(string code, string message, string currentFile, long currentPosition);

        [Obsolete("Use " + nameof(Logger) + " instead.")]
        bool OnNonCriticalError(string code, string message, string currentFile, long currentPosition);

        [Obsolete("remove candidate")]
        bool OnParameterError(ITemplateParameter parameter, string receivedValue, string message, out string newValue);

        bool OnPotentiallyDestructiveChangesDetected(IReadOnlyList<IFileChange> changes, IReadOnlyList<IFileChange> destructiveChanges);

        [Obsolete("remove candidate")]
        void OnSymbolUsed(string symbol, object value);

        [Obsolete("Use " + nameof(Logger) + " instead.")]
        void LogDiagnosticMessage(string message, string category, params string[] details);

        bool TryGetHostParamDefault(string paramName, out string value);

        void VirtualizeDirectory(string path);

        [Obsolete("remove candidate")]
        bool OnConfirmPartialMatch(string name);
    }
}
