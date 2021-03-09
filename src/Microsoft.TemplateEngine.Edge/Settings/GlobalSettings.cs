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
    internal sealed class GlobalSettings : IGlobalSettings, IDisposable
    {
        private readonly Paths _paths;
        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly string _globalSettingsFile;
        private IDisposable? _watcher;

        public GlobalSettings(IEngineEnvironmentSettings environmentSettings, string globalSettingsFile)
        {
            _environmentSettings = environmentSettings ?? throw new ArgumentNullException(nameof(environmentSettings));
            _globalSettingsFile = globalSettingsFile ?? throw new ArgumentNullException(nameof(globalSettingsFile));
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
            token.ThrowIfCancellationRequested();
            // We must use Mutex because we want to lock across different processes that might want to modify this settings file
            return AsyncMutex.WaitAsync($"812CA7F3-7CD8-44B4-B3F0-0159355C0BD5{_globalSettingsFile}".Replace("\\", "_").Replace("/", "_"), token);
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _watcher = null;
        }

        public async Task<IReadOnlyList<TemplatesSourceData>> GetInstalledTemplatesPackagesAsync(CancellationToken cancellationToken)
        {
            if (!_environmentSettings.Host.FileSystem.FileExists(_globalSettingsFile))
            {
                return Array.Empty<TemplatesSourceData>();
            }

            for (int i = 0; i < 5; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    string? textFileContent = _paths.ReadAllText(_globalSettingsFile, "{}");
                    GlobalSettingsData? data = JsonConvert.DeserializeObject<GlobalSettingsData>(textFileContent);
                    return data.Packages ?? Array.Empty<TemplatesSourceData>();
                }
                catch (Exception)
                {
                    if (i == 4)
                    {
                        throw;
                    }
                }
                await Task.Delay(20).ConfigureAwait(false);
            }
            throw new InvalidOperationException();
        }

        public async Task SetInstalledTemplatesPackagesAsync(IReadOnlyList<TemplatesSourceData> packages, CancellationToken cancellationToken)
        {
            string? serializedText = JsonConvert.SerializeObject(new GlobalSettingsData()
            {
                Packages = packages
            });

            for (int i = 0; i < 5; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _paths.WriteAllText(_globalSettingsFile, serializedText);
                    SettingsChanged?.Invoke();
                    return;
                }
                catch (Exception)
                {
                    if (i == 4)
                    {
                        throw;
                    }
                }
                await Task.Delay(20).ConfigureAwait(false);
            }
            throw new InvalidOperationException();
        }
    }
}
