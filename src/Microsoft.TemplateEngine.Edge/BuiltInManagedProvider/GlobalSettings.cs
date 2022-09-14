// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Edge.Settings;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.BuiltInManagedProvider
{
    internal sealed class GlobalSettings : IGlobalSettings, IDisposable
    {
        private const int FileReadWriteRetries = 20;
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
            Log("Global settings:{0} created settings file at {1}.", Marker.ToString(), _globalSettingsFile);
            if (environmentSettings.Environment.GetEnvironmentVariable("TEMPLATE_ENGINE_DISABLE_FILEWATCHER") != "1")
            {
                _watcher = environmentSettings.Host.FileSystem.WatchFileChanges(_globalSettingsFile, FileChanged);
            }
            Log("Global settings:{0} is initialized.", Marker.ToString());
        }

        public event Action? SettingsChanged;

        public Guid Marker { get; } = Guid.NewGuid();

        public async Task<IDisposable> LockAsync(CancellationToken token)
        {
            if (_disposed)
            {
                Log("Global settings:{0} -> LockAsync: already disposed!!!!!", Marker.ToString());
                throw new ObjectDisposedException($"{nameof(GlobalSettings)} -> {Marker.ToString()}");
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
            Log("Global settings:{0} locked settings file.", Marker.ToString());
            return mutex;
        }

        public void Dispose()
        {
            Log("Global settings:{0} -> Dispose started.", Marker.ToString());
            if (_disposed)
            {
                Log("Global settings:{0} -> Already disposed!!!!!", Marker.ToString());
                return;
            }
            _disposed = true;

            _watcher?.Dispose();
            _watcher = null;
            Log("Global settings:{0} -> Marked as disposed and disposed watcher.", Marker.ToString());
        }

        public async Task<IReadOnlyList<TemplatePackageData>> GetInstalledTemplatePackagesAsync(CancellationToken cancellationToken)
        {
            Log("Global settings:{0} -> GetInstalledTemplatePackagesAsync started.", Marker.ToString());
            if (_disposed)
            {
                Log("Global settings:{0} -> GetInstalledTemplatePackagesAsync: already disposed!!!!!", Marker.ToString());
                throw new ObjectDisposedException($"{nameof(GlobalSettings)} -> {Marker.ToString()}");
            }

            if (!_environmentSettings.Host.FileSystem.FileExists(_globalSettingsFile))
            {
                Log("Global settings:{0} -> GetInstalledTemplatePackagesAsync: global settings file not exist!!!", Marker.ToString());
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
                            package.Value<string>(nameof(TemplatePackageData.MountPointUri)) ?? "",
                            ((DateTime?)package[nameof(TemplatePackageData.LastChangeTime)]) ?? default,
                            package.ToStringDictionary(propertyName: nameof(TemplatePackageData.Details))
                        ));
                    }
                    Log("Global settings:{0} -> GetInstalledTemplatePackagesAsync succeeded.", Marker.ToString());

                    return packages;
                }
                catch (Exception)
                {
                    if (i == (FileReadWriteRetries - 1))
                    {
                        Log("Global settings:{0} -> GetInstalledTemplatePackagesAsync: with {1} reties it didn't succeed to get the packages.", Marker.ToString(), FileReadWriteRetries);
                        throw;
                    }
                }
                Log("Global settings:{0} -> GetInstalledTemplatePackagesAsync: with {1} reties continue.", Marker.ToString(), i);
                await Task.Delay(20, cancellationToken).ConfigureAwait(false);
            }
            Log("Global settings:{0} -> GetInstalledTemplatePackagesAsync throws InvalidOperationException!!!!!!!!", Marker.ToString());
            throw new InvalidOperationException();
        }

        public async Task SetInstalledTemplatePackagesAsync(IReadOnlyList<TemplatePackageData> packages, CancellationToken cancellationToken)
        {
            Log("Global settings:{0} -> SetInstalledTemplatePackagesAsync: started.", Marker.ToString());
            if (_disposed)
            {
                Log("Global settings:{0} -> SetInstalledTemplatePackagesAsync: already disposed!!!!!", Marker.ToString());
                throw new ObjectDisposedException($"{nameof(GlobalSettings)} -> {Marker.ToString()}");
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
                    Log("Global settings:{0} -> SetInstalledTemplatePackagesAsync: finished write settings file.", Marker.ToString());
                    if (SettingsChanged == null)
                    {
                        Log("Global settings:{0} -> SetInstalledTemplatePackagesAsync: SettingsChanged is null.", Marker.ToString());
                    }
                    SettingsChanged?.Invoke();
                    return;
                }
                catch (Exception)
                {
                    if (i == (FileReadWriteRetries - 1))
                    {
                        throw;
                    }
                }
                await Task.Delay(20, cancellationToken).ConfigureAwait(false);
            }
            throw new InvalidOperationException();
        }

        public void Log(string? message, params object?[] args)
        {
            _environmentSettings.Host.Logger.LogInformation(message, args);
        }

        private void FileChanged(object sender, FileSystemEventArgs e)
        {
            Log("Global settings:{0} -> FileChanged was triggered.", Marker.ToString());
            if (!_environmentSettings.Host.FileSystem.FileExists(_globalSettingsFile))
            {
                Log("Global settings:{0} -> FileChanged: global settings file not exist!!!", Marker.ToString());
            }
            SettingsChanged?.Invoke();
        }
    }
}
