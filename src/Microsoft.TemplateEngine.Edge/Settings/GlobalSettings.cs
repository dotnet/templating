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

        public GlobalSettings(IEngineEnvironmentSettings environmentSettings, string globalSettingsFile)
        {
            _environmentSettings = environmentSettings;
            _globalSettingsFile = globalSettingsFile;
            _paths = new Paths(environmentSettings);
            environmentSettings.Host.FileSystem.CreateDirectory(Path.GetDirectoryName(_globalSettingsFile));
            _watcher = environmentSettings.Host.FileSystem.WatchFileChanges(_globalSettingsFile, FileChanged);
        }

        private void FileChanged(object sender, FileSystemEventArgs e)
        {
            SettingsChanged?.Invoke();
        }

        public event Action? SettingsChanged;

        public Task<IDisposable> LockAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            return AsyncMutex.WaitAsync($"812CA7F3-7CD8-44B4-B3F0-0159355C0BD5{_globalSettingsFile}".Replace("\\", "_").Replace("/", "_"), token);
        }

        public void Dispose()
        {
            _watcher.Dispose();
        }

        public async Task<IReadOnlyList<TemplatesSourceData>> GetInstalledTemplatesPackagesAsync(CancellationToken cancellationToken)
        {
            if (!_environmentSettings.Host.FileSystem.FileExists(_globalSettingsFile))
                return Array.Empty<TemplatesSourceData>();

            for (int i = 0; i < 5; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new TaskCanceledException();
                try
                {
                    var textFileContent = _paths.ReadAllText(_globalSettingsFile, "{}");
                    var data = JsonConvert.DeserializeObject<GlobalSettingsData>(textFileContent);
                    return data.Packages ?? Array.Empty<TemplatesSourceData>();
                }
                catch (Exception)
                {
                    if (i == 4)
                        throw;
                }
                await Task.Delay(20).ConfigureAwait(false);
            }
            throw new InvalidOperationException();
        }

        public async Task SetInstalledTemplatesPackagesAsync(IReadOnlyList<TemplatesSourceData> packages, CancellationToken cancellationToken)
        {
            var serializedText = JsonConvert.SerializeObject(new GlobalSettingsData()
            {
                Packages = packages
            });

            for (int i = 0; i < 5; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new TaskCanceledException();
                try
                {
                    _paths.WriteAllText(_globalSettingsFile, serializedText);
                    SettingsChanged?.Invoke();
                    return;
                }
                catch (Exception)
                {
                    if (i == 4)
                        throw;
                }
                await Task.Delay(20).ConfigureAwait(false);
            }
            throw new InvalidOperationException();
        }
    }
}
