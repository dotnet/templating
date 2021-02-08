using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;

namespace Microsoft.TemplateEngine.Utils
{
    public class DefaultTemplateEngineHost : ITemplateEngineHost, ITemplateEngineHost2
    {
        private readonly IReadOnlyDictionary<string, string> _hostDefaults;
        private readonly IReadOnlyList<KeyValuePair<Guid, Func<Type>>> _hostBuiltInComponents;
        private ILogger _logger = NullLogger.Instance;
        private static readonly IReadOnlyList<KeyValuePair<Guid, Func<Type>>> NoComponents = Array.Empty<KeyValuePair<Guid, Func<Type>>>();

        public DefaultTemplateEngineHost(string hostIdentifier, string version, string locale)
            : this(hostIdentifier, version, locale, null)
        {
        }

        public DefaultTemplateEngineHost(string hostIdentifier, string version, string locale, Dictionary<string, string> defaults)
            : this (hostIdentifier, version, locale, defaults, NoComponents, null)
        {
        }

        public DefaultTemplateEngineHost(string hostIdentifier, string version, string locale, Dictionary<string, string> defaults, IReadOnlyList<KeyValuePair<Guid, Func<Type>>> builtIns)
            : this(hostIdentifier, version, locale, defaults, builtIns, null)
        {
        }

        public DefaultTemplateEngineHost(string hostIdentifier, string version, string locale, Dictionary<string, string> defaults, IReadOnlyList<string> fallbackHostTemplateConfigNames)
            : this(hostIdentifier, version, locale, defaults, NoComponents, fallbackHostTemplateConfigNames)
        {
        }

        public DefaultTemplateEngineHost(string hostIdentifier, string version, string locale, Dictionary<string, string> defaults, IReadOnlyList<KeyValuePair<Guid, Func<Type>>> builtIns, IReadOnlyList<string> fallbackHostTemplateConfigNames, ILogger logger = null)
        {
            HostIdentifier = hostIdentifier;
            Version = version;
            Locale = locale;
            _hostDefaults = defaults ?? new Dictionary<string, string>();
            FileSystem = new PhysicalFileSystem();
            _hostBuiltInComponents = builtIns ?? NoComponents;
            FallbackHostTemplateConfigNames = fallbackHostTemplateConfigNames ?? new List<string>();

            if (logger != null)
            {
                _logger = logger;
            }
        }

        public IPhysicalFileSystem FileSystem { get; private set; }

        public string Locale { get; private set; }

        public Action<string, TimeSpan, int> OnLogTiming { get; set; }

        public void UpdateLocale(string newLocale)
        {
            Locale = newLocale;
        }

        public string HostIdentifier { get; }

        public IReadOnlyList<string> FallbackHostTemplateConfigNames { get; }

        public string Version { get; }

        public virtual IReadOnlyList<KeyValuePair<Guid, Func<Type>>> BuiltInComponents => _hostBuiltInComponents;

        public ILogger Logger => _logger;

        public virtual void LogMessage(string message)
        {
            Logger.LogInformation(message);
        }

        public virtual void OnCriticalError(string code, string message, string currentFile, long currentPosition)
        {
            StringBuilder logMessage = new StringBuilder();
            logMessage.Append(message);
            if (!string.IsNullOrWhiteSpace(code))
            {
                logMessage.Append($" Error code:{code}.");
            }
            if (!string.IsNullOrWhiteSpace(currentFile))
            {
                logMessage.Append($" File:{currentFile}, position: {currentPosition}.");
            }
            Logger.LogError(logMessage.ToString());
        }

        public virtual bool OnNonCriticalError(string code, string message, string currentFile, long currentPosition)
        {
            StringBuilder logMessage = new StringBuilder();
            logMessage.Append(message);
            if (!string.IsNullOrWhiteSpace(code))
            {
                logMessage.Append($" Error code:{code}.");
            }
            if (!string.IsNullOrWhiteSpace(currentFile))
            {
                logMessage.Append($" File:{currentFile}, position: {currentPosition}.");
            }
            Logger.LogWarning(logMessage.ToString());
            return false;
        }

        public virtual bool OnParameterError(ITemplateParameter parameter, string receivedValue, string message, out string newValue)
        {
            StringBuilder logMessage = new StringBuilder();
            logMessage.Append(message);
            if (parameter != null && !string.IsNullOrWhiteSpace(parameter.Name))
            {
                logMessage.Append($" Parameter name:{parameter.Name}");
                if (!string.IsNullOrWhiteSpace(receivedValue))
                {
                    logMessage.Append($", received value:{receivedValue}.");
                }
                else
                {
                    _ = logMessage.Append('.');
                }
            }
            Logger.LogError(logMessage.ToString());
            newValue = null;
            return false;
        }

        public virtual void OnSymbolUsed(string symbol, object value)
        {
            if (!string.IsNullOrWhiteSpace(symbol))
            {
                Logger.LogDebug($"The symbol {symbol} was used with value '{value}'");
            }
        }

        // stub that will be built out soon.
        public virtual bool TryGetHostParamDefault(string paramName, out string value)
        {
            switch (paramName)
            {
                case "HostIdentifier":
                    value = HostIdentifier;
                    return true;
            }

            return _hostDefaults.TryGetValue(paramName, out value);
        }

        public void VirtualizeDirectory(string path)
        {
            FileSystem = new InMemoryFileSystem(path, FileSystem);
        }

        public bool OnPotentiallyDestructiveChangesDetected(IReadOnlyList<IFileChange> changes, IReadOnlyList<IFileChange> destructiveChanges)
        {
            if (!destructiveChanges.Any())
            {
                return true;
            }
            string logMessage = string.Join(";", destructiveChanges.Select(change => $"target path: {change.TargetRelativePath}, change kind: {change.ChangeKind}"));
            Logger.LogWarning($"Potentially destructive changes detected: {logMessage}.");
            return true;
        }

        public bool OnConfirmPartialMatch(string name)
        {
            return true;
        }

        [Obsolete("The method is obsolete. All diagnostic messages will be logged using Logger using Debug level.")]
        public void RegisterDiagnosticLogger(string category, Action<string, string[]> messageHandler)
        {
            //do nothing
        }

        public void LogDiagnosticMessage(string message, string category, params string[] details)
        {
            Logger.LogDebug($"[{category}] {message}", details);
        }

        public void LogTiming(string label, TimeSpan duration, int depth)
        {
            OnLogTiming?.Invoke(label, duration, depth);
        }
    }
}
