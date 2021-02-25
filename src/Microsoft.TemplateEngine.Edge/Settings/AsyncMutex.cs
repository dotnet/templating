// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    internal class AsyncMutex
    {
        private readonly TaskCompletionSource<AsyncMutex> _taskCompletionSource;
        private readonly ManualResetEvent _blockReleasingMutex = new ManualResetEvent(false);
        private readonly string _mutexName;
        private readonly CancellationToken _token;
        private readonly Func<CancellationToken, Task>? _unlockCallback;

        private AsyncMutex(string mutexName, CancellationToken token, Func<CancellationToken, Task>? unlockCallback)
        {
            _mutexName = mutexName;
            _token = token;
            _unlockCallback = unlockCallback;
            _taskCompletionSource = new TaskCompletionSource<AsyncMutex>();

            var thread = new Thread(new ThreadStart(WaitLoop));
            thread.IsBackground = true;
            thread.Start();
            thread.Name = "TemplateEngine AsyncMutex";
        }

        public static Task<AsyncMutex> WaitAsync(string mutexName, CancellationToken token, Func<CancellationToken, Task>? unlockCallback)
        {
            var mutex = new AsyncMutex(mutexName, token, unlockCallback);
            return mutex._taskCompletionSource.Task;
        }

        private void WaitLoop()
        {
            var mutex = new Mutex(false, _mutexName);
            while (true)
            {
                if (_token.IsCancellationRequested)
                {
                    _taskCompletionSource.SetCanceled();
                    return;
                }
                if (mutex.WaitOne(100))
                {
                    //Check if we were cancalled while waiting for mutex...
                    if (_token.IsCancellationRequested)
                    {
                        mutex.ReleaseMutex();
                        _taskCompletionSource.SetCanceled();
                        return;
                    }
                    break;
                }
            }
            _taskCompletionSource.SetResult(this);
            _blockReleasingMutex.WaitOne();
            _blockReleasingMutex.Dispose();
            mutex.ReleaseMutex();
        }

        public async Task ReleaseMutexAsync(CancellationToken token)
        {
            if (_unlockCallback != null)
            {
                await _unlockCallback(token).ConfigureAwait(false);
            }
            _blockReleasingMutex.Set();
        }
    }
}
