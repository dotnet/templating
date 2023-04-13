// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Installer;
using Microsoft.TemplateEngine.Edge.Installers.NuGet;
using Microsoft.TemplateEngine.Edge.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Edge.BuiltInManagedProvider
{
    internal sealed class GlobalSettings : IGlobalSettings, IDisposable
    {
        private const int FileReadWriteRetries = 20;
        private const int MillisecondsInterval = 20;
        private static readonly TimeSpan MaxNotificationDelayOnWriterLock = TimeSpan.FromSeconds(1);
        private readonly SettingsFilePaths _paths;
        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly string _globalSettingsFile;
        private readonly ILogger _logger;
        private IDisposable? _watcher;
        private volatile bool _disposed;
        private volatile AsyncMutex? _mutex;
        private int _waitingInstances;

        public GlobalSettings(IEngineEnvironmentSettings environmentSettings, string globalSettingsFile)
        {
            _environmentSettings = environmentSettings ?? throw new ArgumentNullException(nameof(environmentSettings));
            _globalSettingsFile = globalSettingsFile ?? throw new ArgumentNullException(nameof(globalSettingsFile));
            _paths = new SettingsFilePaths(environmentSettings);
            _logger = _environmentSettings.Host.LoggerFactory.CreateLogger<GlobalSettings>();
            environmentSettings.Host.FileSystem.CreateDirectory(Path.GetDirectoryName(_globalSettingsFile));

            _watcher = CreateWatcherIfRequested();
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

                    return _environmentSettings.Host.FileSystem.FileExists(_paths.FirstRunCookie)
                        ? packages
                        : await MigrateCacheMetadataAsync(packages, cancellationToken).ConfigureAwait(false);
                }
                catch (JsonReaderException ex)
                {
                    var wrappedEx = new JsonReaderException(string.Format(LocalizableStrings.GlobalSettings_Error_CorruptedSettings, _globalSettingsFile, ex.Message), ex);
                    throw wrappedEx;
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
                    // Ignore FSW notifications received during writing changes (we'll notify synchronously)
                    _watcher?.Dispose();
                    _environmentSettings.Host.FileSystem.WriteObject(_globalSettingsFile, globalSettingsData);
                    // We are ready for new notifications now
                    _watcher = CreateWatcherIfRequested();
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
                await Task.Delay(MillisecondsInterval, cancellationToken).ConfigureAwait(false);
            }
            throw new InvalidOperationException();
        }

        private IDisposable? CreateWatcherIfRequested()
        {
            if (_environmentSettings.Environment.GetEnvironmentVariable("TEMPLATE_ENGINE_DISABLE_FILEWATCHER") != "1")
            {
                return _environmentSettings.Host.FileSystem.WatchFileChanges(_globalSettingsFile, FileChanged);
            }

            return null;
        }

        // This method is called whenever there is a change in global settings. Since the handlers of SettingsChanged event
        //  first grab the lock (LockAsync) and then read the whole content of GlobalSettings folder - we are here making sure
        //  to skip unwanted extra calls - all concurrent calls while handler is waiting for a lock leads to duplicate reprocessing
        //  of a whole global settings folder.
        //  To prevent this - we try to wait for a lock on behalf of the handler and refuse all concurrent file change notifications in the meantime
        private async void FileChanged(object sender, FileSystemEventArgs e)
        {
            // Make sure the waiting happens only for one notification at the time - as we do not care about other notifications
            // until the SettingsChanged is called
            //  if multiple concurrent call(s) get here, while there is already other caller inside waiting for the lock
            //  those concurrent callers will just return (as counter is 1 already).
            if (Interlocked.Increment(ref _waitingInstances) > 1)
            {
                return;
            }

            await TryWaitForLock().ConfigureAwait(false);

            // We are ready for new notifications now - indicate so by clearing the counter
            Interlocked.Exchange(ref _waitingInstances, 0);

            SettingsChanged?.Invoke();
        }

        private async Task<bool> TryWaitForLock()
        {
            CancellationTokenSource cts = new();
            try
            {
                cts.CancelAfter(MaxNotificationDelayOnWriterLock);
                if (!(_mutex?.IsLocked ?? false))
                {
                    using (await LockAsync(cts.Token).ConfigureAwait(false))
                    { }
                }
            }
            catch (Exception e)
            {
                _environmentSettings.Host.Logger.LogDebug(
                    "Failed to wait for GlobalSettings lock to be freed, before notifying about new changes. {error}",
                    e.Message);
                return false;
            }

            return true;
        }

        private async Task<IReadOnlyList<TemplatePackageData>> MigrateCacheMetadataAsync(IReadOnlyList<TemplatePackageData> packages, CancellationToken cancellationToken)
        {
            _logger.LogWarning(LocalizableStrings.GlobalSettingsTemplatePackageProvider_DataMigrationWarning);

            string sourceFeedKey = "NuGetSource";
            string packageIdKey = "PackageId";
            string versionKey = "Version";
            string ownersKey = "Owners";
            string trustedKey = "Trusted";

            var updatedPackages = new List<TemplatePackageData>();
            var metadataDownloader = new NuGetApiPackageManager(_environmentSettings);

            foreach (var package in packages)
            {
                string? packageSource = string.Empty;
                package.Details?.TryGetValue(sourceFeedKey, out packageSource);

                string? identifier = string.Empty;
                package.Details?.TryGetValue(packageIdKey, out identifier);

                string? version = string.Empty;
                package.Details?.TryGetValue(versionKey, out version);

                if (!string.IsNullOrWhiteSpace(packageSource))
                {
                    var updatedPackageMetadata = await metadataDownloader.GetPackageMetadataAsync(
                        identifier,
                        version,
                        packageSource,
                        CancellationToken.None).ConfigureAwait(false);

                    var extendedPackageDetails = package.Details.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    extendedPackageDetails[trustedKey] = updatedPackageMetadata.Trusted.ToString() ?? false.ToString();
                    extendedPackageDetails[ownersKey] = updatedPackageMetadata.Owners;
                    updatedPackages.Add(new TemplatePackageData(package.InstallerId, package.MountPointUri, package.LastChangeTime, extendedPackageDetails));
                }
                else
                {
                    updatedPackages.Add(package);
                }
            }

            using var disposable = await LockAsync(cancellationToken).ConfigureAwait(false);
            // cleanup existing cache file
            await SetInstalledTemplatePackagesAsync(new List<TemplatePackageData>(), CancellationToken.None).ConfigureAwait(false);
            await SetInstalledTemplatePackagesAsync(updatedPackages, CancellationToken.None).ConfigureAwait(false);

            _environmentSettings.Host.FileSystem.CreateFile(_paths.FirstRunCookie);

            return updatedPackages;
        }
    }
}
