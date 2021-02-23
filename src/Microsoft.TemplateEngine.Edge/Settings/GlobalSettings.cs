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
using NuGet.Common;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    class GlobalSettings : IGlobalSettings, IDisposable
    {
        private readonly Paths _paths;
        private readonly IEngineEnvironmentSettings _environmentSettings;
        private readonly string _globalSettingsFile;
        private bool _locked;
        private IDisposable _watcher;

        public GlobalSettings(IEngineEnvironmentSettings environmentSettings, string globalSettingsFile)
        {
            _environmentSettings = environmentSettings;
            _paths = new Paths(environmentSettings);
            _globalSettingsFile = globalSettingsFile;
            environmentSettings.Host.FileSystem.CreateDirectory(Path.GetDirectoryName(_globalSettingsFile));
            _watcher = environmentSettings.Host.FileSystem.WatchFileChanges(_globalSettingsFile, FileChanged);
            ReloadSettings(false, null);
            UserInstalledTemplatesSources = _userInstalledTemplatesSources.ToArray();
        }

        void ReloadSettings(bool triggerEvent, Stream? existingStream)
        {
            GlobalSettingsData? loaded;
            try
            {
                string textFileContent;
                if (existingStream == null)
                {
                    textFileContent = _paths.ReadAllText(_globalSettingsFile);
                }
                else
                {
                    using (var reader = new StreamReader(existingStream, Encoding.UTF8, true, 4096, true))
                        textFileContent = reader.ReadToEnd();
                }
                loaded = JsonConvert.DeserializeObject<GlobalSettingsData>(textFileContent);
            }
            catch (Exception ex)
            {
                _environmentSettings.Host.OnNonCriticalError(null, ex.ToString(), null, 0);
                loaded = null;
            }

            bool settingsChanged = false;
            if (loaded == null)
            {
                if (_userInstalledTemplatesSources.Count > 0)
                    settingsChanged = true;
                _userInstalledTemplatesSources.Clear();
            }
            else
            {
                _userInstalledTemplatesSources = loaded.UserInstalledTemplatesSources;
                //TODO: Do proper compare if anything changed
                settingsChanged = true;
            }

            UserInstalledTemplatesSources = _userInstalledTemplatesSources.ToArray();

            if (triggerEvent && settingsChanged)
                SettingsChanged?.Invoke();
        }

        private void FileChanged(object sender, FileSystemEventArgs e)
        {
            //We are in process of modifying settings, ignore file watcher
            if (!_locked)
                ReloadSettings(true, null);
        }

        public event Action? SettingsChanged;

        private List<TemplatesSourceData> _userInstalledTemplatesSources = new List<TemplatesSourceData>();

        public IReadOnlyList<TemplatesSourceData> UserInstalledTemplatesSources { get; private set; }

        public void Add(TemplatesSourceData userInstalledTemplate)
        {
            if (!_locked)
                throw new InvalidOperationException($"Call {nameof(LockAsync)} before calling this method");
            _userInstalledTemplatesSources.RemoveAll(data => data.MountPointUri == userInstalledTemplate.MountPointUri);
            _userInstalledTemplatesSources.Add(userInstalledTemplate);
        }

        public void Remove(TemplatesSourceData userInstalledTemplate)
        {
            if (!_locked)
                throw new InvalidOperationException($"Call {nameof(LockAsync)} before calling this method");
            _userInstalledTemplatesSources.RemoveAll(data => data.MountPointUri == userInstalledTemplate.MountPointUri);
        }

        class MyMutex
        {
            TaskCompletionSource<bool> _taskCompletionSource;
            CancellationToken _token;
            ManualResetEvent _mre = new ManualResetEvent(false);

            public MyMutex(CancellationToken token)
            {
                _token = token;
                _taskCompletionSource = new TaskCompletionSource<bool>();
                var thread = new Thread(new ThreadStart(WaitLoop));
                thread.IsBackground = true;
                thread.Start();
            }

            public Task WaitAsync()
            {
                return _taskCompletionSource.Task;
            }

            private void WaitLoop()
            {
                var mutex = new Mutex(false, "{01B5E8B0-EF76-48ED-BE95-A0458D7DA2C2}");
                while (true)
                {
                    if (_token.IsCancellationRequested)
                    {
                        _taskCompletionSource.SetCanceled();
                        return;
                    }
                    if (mutex.WaitOne(20))
                        break;
                }
                _taskCompletionSource.SetResult(true);
                _mre.WaitOne();
                mutex.ReleaseMutex();
            }
            public void Release()
            {
                _mre.Set();
            }
        }

        class DisposableCallback : IDisposable
        {
            private Action<Stream, MyMutex?>? _disposeCalled;
            private Stream _fileStream;
            private MyMutex? _mutex;

            public DisposableCallback(Action<Stream, MyMutex?> disposeCalled, Stream fileStream, MyMutex? mutex)
            {
                _disposeCalled = disposeCalled;
                _fileStream = fileStream;
                _mutex = mutex;
            }

            public void Dispose()
            {
                var disposeCalled = Interlocked.Exchange(ref _disposeCalled, null);
                disposeCalled?.Invoke(_fileStream, _mutex);
            }
        }

        public async Task<IDisposable> LockAsync(CancellationToken token)
        {
            while (true)
            {
                if (token.IsCancellationRequested)
                    throw new TaskCanceledException();

                try
                {
                    MyMutex? mutex = null;
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        mutex = new MyMutex(token);
                        await mutex.WaitAsync();
                    }
                    var stream = _environmentSettings.Host.FileSystem.CreateFileStream(_globalSettingsFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                    _locked = true;
                    ReloadSettings(true, stream);
                    return new DisposableCallback(Unlock, stream, mutex);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }

                await Task.Delay(20, token).ConfigureAwait(false);
            }
        }

        private void Unlock(Stream stream, MyMutex? mutex)
        {
            try
            {
                UserInstalledTemplatesSources = _userInstalledTemplatesSources.ToArray();
                stream.SetLength(0);//Delete existing content
                using (var streamWriter = new StreamWriter(stream))
                {
                    streamWriter.Write(JsonConvert.SerializeObject(new GlobalSettingsData()
                    {
                        UserInstalledTemplatesSources = _userInstalledTemplatesSources
                    }, Formatting.Indented));
                }
                _locked = false;
            }
            finally
            {
                stream.Dispose();
                mutex?.Release();
                SettingsChanged?.Invoke();
            }
        }

        public void Dispose()
        {
            if (_locked)
                throw new Exception("Locked during dispose");
            _watcher.Dispose();
        }
    }
}
