﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Edge.Settings
{
    /// <summary>
    /// Helper class to work with <see cref="Mutex"/> in <c>async</c> method, since <c>await</c>
    /// can switch to different thread and <see cref="Mutex.ReleaseMutex"/> must be called from same thread.
    /// Hence this helper class.
    /// </summary>
    internal sealed class AsyncMutex : IDisposable
    {
        private readonly TaskCompletionSource<IDisposable> _taskCompletionSource;
        private readonly ManualResetEvent _blockReleasingMutex = new ManualResetEvent(false);
        private readonly string _mutexName;
        private readonly CancellationToken _token;
        private bool _disposed;

        private AsyncMutex(string mutexName, CancellationToken token)
        {
            _mutexName = mutexName;
            _token = token;
            _taskCompletionSource = new TaskCompletionSource<IDisposable>();

            var thread = new Thread(new ThreadStart(WaitLoop));
            thread.IsBackground = true;
            thread.Start();
            thread.Name = "TemplateEngine AsyncMutex";
        }

        public static Task<IDisposable> WaitAsync(string mutexName, CancellationToken token)
        {
            var mutex = new AsyncMutex(mutexName, token);
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
                    _blockReleasingMutex.Dispose();
                    return;
                }
                if (mutex.WaitOne(100))
                {
                    //Check if we were cancalled while waiting for mutex...
                    if (_token.IsCancellationRequested)
                    {
                        mutex.ReleaseMutex();
                        _taskCompletionSource.SetCanceled();
                        _blockReleasingMutex.Dispose();
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

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            _blockReleasingMutex.Set();
        }
    }
}
