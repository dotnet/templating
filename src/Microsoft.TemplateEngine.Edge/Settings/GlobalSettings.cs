// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.GlobalSettings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Common;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    internal class GlobalSettings : IGlobalSettings, IDisposable
    {
        private readonly Paths _paths;
        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly string _globalSettingsFile;
        private IDisposable _watcher;
        private AsyncMutex? _mutex;

        public GlobalSettings(IEngineEnvironmentSettings environmentSettings, string globalSettingsFile)
        {
            _environmentSettings = environmentSettings;
            _globalSettingsFile = globalSettingsFile;
            _paths = new Paths(environmentSettings);
            environmentSettings.Host.FileSystem.CreateDirectory(Path.GetDirectoryName(_globalSettingsFile));
            _watcher = environmentSettings.Host.FileSystem.WatchFileChanges(_globalSettingsFile, FileChanged);
        }

        public async Task ReloadSettings(bool triggerEvent, CancellationToken token)
        {
            var loaded = await LoadDataAsync(token).ConfigureAwait(false);

            bool settingsChanged = false;
            if (loaded == null)
            {
                if (_userInstalledTemplatesSources.Count > 0)
                {
                    settingsChanged = true;
                }
                _userInstalledTemplatesSources = new List<TemplatesSourceData>();
            }
            else
            {
                _userInstalledTemplatesSources = loaded.UserInstalledTemplatesSources;
                //TODO: Do proper compare if anything changed
                settingsChanged = true;
            }

            UserInstalledTemplatesSources = _userInstalledTemplatesSources.ToArray();

            if (triggerEvent && settingsChanged)
            {
                SettingsChanged?.Invoke();
            }
        }

        private async Task<GlobalSettingsData?> LoadDataAsync(CancellationToken token)
        {
            if (!_environmentSettings.Host.FileSystem.FileExists(_globalSettingsFile))
                return null;
            string textFileContent;

            while (true)
            {
                try
                {
                    using (var fileStream = _environmentSettings.Host.FileSystem.CreateFileStream(_globalSettingsFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var reader = new StreamReader(fileStream))
                        textFileContent = await reader.ReadToEndAsync().ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<GlobalSettingsData>(textFileContent);
                }
                catch (Exception)
                {
                    if (token.IsCancellationRequested)
                    {
                        throw;
                    }
                }
                await Task.Delay(20).ConfigureAwait(false);
            }
        }

        private async Task SaveDataAsync(CancellationToken token)
        {
            var serializedText = JsonConvert.SerializeObject(new GlobalSettingsData()
            {
                UserInstalledTemplatesSources = _userInstalledTemplatesSources
            });

            while (true)
            {
                try
                {
                    using var fileStream = _environmentSettings.Host.FileSystem.CreateFileStream(_globalSettingsFile, FileMode.Create, FileAccess.Write, FileShare.None);
                    using var writer = new StreamWriter(fileStream);
                    await writer.WriteAsync(serializedText).ConfigureAwait(false);
                    return;
                }
                catch (Exception)
                {
                    if (token.IsCancellationRequested)
                    {
                        throw;
                    }
                    await Task.Delay(20).ConfigureAwait(false);
                }
            }
        }

        private async void FileChanged(object sender, FileSystemEventArgs e)
        {
            //We are in process of modifying settings, ignore file watcher
            if (_mutex == null)
            {
                try
                {
                    using var cancellationTokenSource = new CancellationTokenSource(1000);
                    await ReloadSettings(true, cancellationTokenSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _environmentSettings.Host.OnCriticalError(null, $"Error while reloading \"{_globalSettingsFile}\" triggered by filewatcher.{ex}", null, 0);
                }
            }
        }

        public event Action? SettingsChanged;

        private List<TemplatesSourceData> _userInstalledTemplatesSources = new List<TemplatesSourceData>();

        public IReadOnlyList<TemplatesSourceData> UserInstalledTemplatesSources { get; private set; } = new TemplatesSourceData[0];

        public void Add(TemplatesSourceData userInstalledTemplate)
        {
            if (_mutex == null)
            {
                throw new InvalidOperationException($"Call {nameof(LockAsync)} before calling this method");
            }
            _userInstalledTemplatesSources.RemoveAll(data => data.MountPointUri == userInstalledTemplate.MountPointUri);
            _userInstalledTemplatesSources.Add(userInstalledTemplate);
        }

        public void Remove(TemplatesSourceData userInstalledTemplate)
        {
            if (_mutex == null)
            {
                throw new InvalidOperationException($"Call {nameof(LockAsync)} before calling this method");
            }
            _userInstalledTemplatesSources.RemoveAll(data => data.MountPointUri == userInstalledTemplate.MountPointUri);
        }

        public async Task LockAsync(CancellationToken token)
        {
            if (_mutex is AsyncMutex)
            {
                throw new InvalidOperationException($"{nameof(LockAsync)} called while already locked.");
            }
            if (token.IsCancellationRequested) {
                throw new TaskCanceledException();
            }

            _mutex = await AsyncMutex.WaitAsync($"812CA7F3-7CD8-44B4-B3F0-0159355C0BD5{_globalSettingsFile}".Replace("\\", "_").Replace("/", "_"), token, null);
            try
            {
                await ReloadSettings(false, token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await _mutex.ReleaseMutexAsync(token);
                _mutex = null;
                throw;
            }
        }

        public async Task UnlockAsync(CancellationToken token)
        {
            if(!(_mutex is AsyncMutex mutex))
            {
                throw new InvalidOperationException($"{nameof(UnlockAsync)} called while already unlocked.");
            }

            try
            {
                await SaveDataAsync(token).ConfigureAwait(false);
                await ReloadSettings(true, token).ConfigureAwait(false);
            }
            finally
            {
                await _mutex!.ReleaseMutexAsync(token).ConfigureAwait(false);
                _mutex = null;
            }
        }

        public void Dispose()
        {
            if (_mutex != null)
                throw new Exception("Locked during dispose");
            _watcher.Dispose();
        }
    }
}
