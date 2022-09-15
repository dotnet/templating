// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TemplateEngine.Utils
{
    /// <summary>
    /// Provides support for asynchronous lazy initialization.
    /// </summary>
    /// <typeparam name="T">The type to be lazily initialized.</typeparam>
    public class AsyncLazy<T> : Lazy<Task<T>>
    {
        // inspired by https://devblogs.microsoft.com/pfxteam/asynclazyt/
        public AsyncLazy(Func<T> valueFactory)
            : base(() => Task.Factory.StartNew(valueFactory, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default))
        { }

        public AsyncLazy(Func<Task<T>> taskFactory)
            : base(() => Task.Factory.StartNew(
                () => taskFactory(), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default)
                .Unwrap())
        { }

        public TaskAwaiter<T> GetAwaiter() { return Value.GetAwaiter(); }
    }
}
