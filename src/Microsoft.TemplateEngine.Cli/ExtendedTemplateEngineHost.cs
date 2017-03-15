﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;

namespace Microsoft.TemplateEngine.Cli
{
    internal class ExtendedTemplateEngineHost : ITemplateEngineHost
    {
        private readonly New3Command _new3Command;
        private readonly ITemplateEngineHost _baseHost;

        public ExtendedTemplateEngineHost(ITemplateEngineHost baseHost, New3Command new3Command)
        {
            _baseHost = baseHost;
            _new3Command = new3Command;
        }

        public IPhysicalFileSystem FileSystem => _baseHost.FileSystem;

        public string Locale => _baseHost.Locale;

        public void UpdateLocale(string newLocale) => _baseHost.UpdateLocale(newLocale);

        public string HostIdentifier => _baseHost.HostIdentifier;

        public IReadOnlyList<string> FallbackHostTemplateConfigNames => _baseHost.FallbackHostTemplateConfigNames;

        public string Version => _baseHost.Version;

        public virtual IReadOnlyList<KeyValuePair<Guid, Func<Type>>> BuiltInComponents => _baseHost.BuiltInComponents;

        public virtual void LogMessage(string message) => _baseHost.LogMessage(message);

        public virtual void OnCriticalError(string code, string message, string currentFile, long currentPosition)
        {
            _baseHost.OnCriticalError(code, message, currentFile, currentPosition);
        }

        public virtual bool OnNonCriticalError(string code, string message, string currentFile, long currentPosition)
        {
            return _baseHost.OnNonCriticalError(code, message, currentFile, currentPosition);
        }

        public virtual bool OnParameterError(ITemplateParameter parameter, string receivedValue, string message, out string newValue)
        {
            return _baseHost.OnParameterError(parameter, receivedValue, message, out newValue);
        }

        public virtual void OnSymbolUsed(string symbol, object value) => _baseHost.OnSymbolUsed(symbol, value);

        public virtual void OnTimingCompleted(string label, TimeSpan timing) => _baseHost.OnTimingCompleted(label, timing);

        public virtual bool TryGetHostParamDefault(string paramName, out string value)
        {
            switch (paramName)
            {
                case "GlobalJsonExists":
                    value = GlobalJsonFileExistsInPath.ToString();
                    return true;
                default:
                    return _baseHost.TryGetHostParamDefault(paramName, out value);
            }
        }

        public void VirtualizeDirectory(string path)
        {
            _baseHost.VirtualizeDirectory(path);
        }

        private static string GetChangeString(ChangeKind kind)
        {
            string changeType;

            switch (kind)
            {
                case ChangeKind.Change:
                    changeType = Strings.Change;
                    break;
                case ChangeKind.Delete:
                    changeType = Strings.Delete;
                    break;
                case ChangeKind.Overwrite:
                    changeType = Strings.Overwrite;
                    break;
                default:
                    changeType = Strings.UnknownChangeKind;
                    break;
            }

            return changeType;
        }

        public bool OnPotentiallyDestructiveChangesDetected(IReadOnlyList<IFileChange> changes, IReadOnlyList<IFileChange> destructiveChanges)
        {
            Reporter.Error.WriteLine(Strings.DestructiveChangesNotification.Bold().Red());
            int longestChangeTextLength = destructiveChanges.Max(x => GetChangeString(x.ChangeKind).Length);
            int padLen = 5 + longestChangeTextLength;

            foreach (IFileChange change in destructiveChanges)
            {
                string changeKind = GetChangeString(change.ChangeKind);
                Reporter.Error.WriteLine(($"  {changeKind}".PadRight(padLen) + change.TargetRelativePath).Bold().Red());
            }
            
            Reporter.Error.WriteLine();
            Reporter.Error.WriteLine(Strings.RerunCommandAndPassForceToCreateAnyway.Bold().Red());
            return false;
        }

        public bool OnConfirmPartialMatch(string name)
        {
            return true;
        }

        private bool GlobalJsonFileExistsInPath
        {
            get
            {
                const string fileName = "global.json";

                string workingPath = Path.Combine(FileSystem.GetCurrentDirectory(), _new3Command.OutputPath);
                bool found = false;

                do
                {
                    string checkPath = Path.Combine(workingPath, fileName);
                    found = FileSystem.FileExists(checkPath);
                    if (!found)
                    {
                        workingPath = Path.GetDirectoryName(workingPath.TrimEnd('/', '\\'));

                        if (!FileSystem.DirectoryExists(workingPath))
                        {
                            workingPath = null;
                        }
                    }
                } while (!found && (workingPath != null));

                return found;
            }
        }
    }
}
