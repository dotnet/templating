// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Edge.Settings;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.BuiltInManagedProvider
{
    internal sealed class GlobalSettings : IGlobalSettings, IDisposable
    {
        private const int FileReadWriteRetries = 20;
        private const int MillisecondsInterval = 20;
        private readonly SettingsFilePaths _paths;
        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly string _globalSettingsFile;
        private IDisposable? _watcher;
        private volatile bool _disposed;
        private volatile AsyncMutex? _mutex;

        public GlobalSettings(IEngineEnvironmentSettings environmentSettings, string globalSettingsFile)
        {
            _environmentSettings = environmentSettings ?? throw new ArgumentNullException(nameof(environmentSettings));
            _globalSettingsFile = globalSettingsFile ?? throw new ArgumentNullException(nameof(globalSettingsFile));
            _paths = new SettingsFilePaths(environmentSettings);
            environmentSettings.Host.FileSystem.CreateDirectory(Path.GetDirectoryName(_globalSettingsFile));
            if (environmentSettings.Environment.GetEnvironmentVariable("TEMPLATE_ENGINE_DISABLE_FILEWATCHER") != "1")
            {
                _watcher = environmentSettings.Host.FileSystem.WatchFileChanges(_globalSettingsFile, FileChanged);
            }
        }

        public event Action? SettingsChanged;

        public async Task<IDisposable> LockAsync(CancellationToken token)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(GlobalSettings));
            }
            token.ThrowIfCancellationRequested();
            if (_mutex?.IsLocked ?? false)
            {
                throw new InvalidOperationException("Lock is already taken.");
            }
            // We must use Mutex because we want to lock across different processes that might want to modify this settings file
            var escapedFilename = _globalSettingsFile.Replace("\\", "_").Replace("/", "_");
            var mutex = await AsyncMutex.WaitAsync($"Global\\812CA7F3-7CD8-44B4-B3F0-0159355C0BD5{escapedFilename}", token).ConfigureAwait(false);
            _mutex = mutex;
            return mutex;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _watcher?.Dispose();
            _disposed = true;
            _watcher = null;
        }

        public async Task<IReadOnlyList<TemplatePackageData>> GetInstalledTemplatePackagesAsync(CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(GlobalSettings));
            }

            if (!_environmentSettings.Host.FileSystem.FileExists(_globalSettingsFile))
            {
                return Array.Empty<TemplatePackageData>();
            }

            for (int i = 0; i < FileReadWriteRetries; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var jObject = _environmentSettings.Host.FileSystem.ReadObject(_globalSettingsFile);
                    var packages = new List<TemplatePackageData>();

                    foreach (var package in jObject.Get<JArray>(nameof(GlobalSettingsData.Packages)) ?? new JArray())
                    {
                        packages.Add(new TemplatePackageData(
                            package.ToGuid(nameof(TemplatePackageData.InstallerId)),
                            package.Value<string>(nameof(TemplatePackageData.MountPointUri)) ?? string.Empty,
                            ((DateTime?)package[nameof(TemplatePackageData.LastChangeTime)]) ?? default,
                            package.ToStringDictionary(propertyName: nameof(TemplatePackageData.Details))));
                    }

                    return packages;
                }
                catch (Exception)
                {
                    if (i == (FileReadWriteRetries - 1))
                    {
                        throw;
                    }
                }
                await Task.Delay(MillisecondsInterval, cancellationToken).ConfigureAwait(false);
            }
            throw new InvalidOperationException();
        }

        public async Task SetInstalledTemplatePackagesAsync(IReadOnlyList<TemplatePackageData> packages, CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(GlobalSettings));
            }

            if (!(_mutex?.IsLocked ?? false))
            {
                throw new InvalidOperationException($"Before calling {nameof(SetInstalledTemplatePackagesAsync)}, {nameof(LockAsync)} must be called.");
            }

            var globalSettingsData = new GlobalSettingsData(packages);

            for (int i = 0; i < FileReadWriteRetries; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _environmentSettings.Host.FileSystem.WriteObject(_globalSettingsFile, globalSettingsData);
                    return;
                }
                catch (Exception)
                {
                    if (i == (FileReadWriteRetries - 1))
                    {
                        throw;
                    }
                }
                await Task.Delay(MillisecondsInterval, cancellationToken).ConfigureAwait(false);
            }
            throw new InvalidOperationException();
        }

        private async void FileChanged(object sender, FileSystemEventArgs e)
        {
            // File change might be notified while file is still in progress of being changed.
            // To make sure file change is done, block the notification until sucessfully locking the file once.
            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;
            IDisposable? mutex = null;
            bool shouldBreak = false;
            for (int i = 0; i < FileReadWriteRetries; i++)
            {
                try
                {
                    mutex = await LockAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex is ObjectDisposedException)
                    {
                        // This means file change should finish and the notification should be executed anyway.
                        shouldBreak = true;
                    }
                    if (ex is OperationCanceledException)
                    {
                        // This should not block the notification though locking the file is canceled.
                        shouldBreak = true;
                    }

                    if (i == (FileReadWriteRetries - 1))
                    {
                        throw;
                    }
                    if (ex is InvalidOperationException)
                    {
                        // Potentially the file is still in progress of being changed and
                        // the file is locked. Then this exception is thrown from LockAsync()
                        continue;
                    }
                }
                finally
                {
                    if (mutex != null)
                    {
                        // Successfully locking the file means the file change was done before this lock.
                        // Notification should continue.
                        mutex.Dispose();
                        shouldBreak = true;
                    }
                }
                if (shouldBreak)
                {
                    break;
                }
                await Task.Delay(MillisecondsInterval, cancellationToken).ConfigureAwait(false);
            }

            SettingsChanged?.Invoke();
        }
    }
}
