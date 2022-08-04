﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private readonly TaskCompletionSource<AsyncMutex> _taskCompletionSource;
        private readonly ManualResetEvent _blockReleasingMutex = new ManualResetEvent(false);
        private readonly string _mutexName;
        private readonly CancellationToken _token;
        private volatile bool _disposed;
        private volatile bool _isLocked = true;

        private AsyncMutex(string mutexName, CancellationToken token)
        {
            _mutexName = mutexName;
            _token = token;
            _taskCompletionSource = new TaskCompletionSource<AsyncMutex>();
            new Thread(WaitLoop).Start();
        }

        /// <summary>
        /// Returns true if the mutex is acquired.
        /// </summary>
        public bool IsLocked { get { return _isLocked; } }

        /// <summary>
        /// Creates the <see cref="AsyncMutex"/> and task for waiting until underlying <see cref="Mutex"/> is acquired.
        /// </summary>
        /// <param name="mutexName">The mutex name. The name is case-sensitive.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>created <see cref="AsyncMutex"/>.</returns>
        public static Task<AsyncMutex> WaitAsync(string mutexName, CancellationToken token)
        {
            var mutex = new AsyncMutex(mutexName, token);
            return mutex._taskCompletionSource.Task;
        }

        /// <summary>
        /// Disposes the <see cref="AsyncMutex"/>. If disposed, the underlying <see cref="Mutex"/> is released.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _isLocked = false;

            _blockReleasingMutex.Set();
        }

        private void WaitLoop(object state)
        {
            var mutex = new Mutex(false, _mutexName);
            var mutexAcquired = false;
            try
            {
                while (true)
                {
                    if (_token.IsCancellationRequested)
                    {
                        _taskCompletionSource.SetCanceled();
                        return;
                    }
                    if (mutex.WaitOne(100))
                    {
                        mutexAcquired = true;
                        break;
                    }
                }
                _taskCompletionSource.SetResult(this);
                _blockReleasingMutex.WaitOne();
            }
            finally
            {
                if (mutexAcquired)
                {
                    mutex.ReleaseMutex();
                }
                mutex.Dispose();
                _blockReleasingMutex.Dispose();
            }
        }
    }
}
