// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Core
{
    public class ValueReadEventArgs : EventArgs, IValueReadEventArgs
    {
        public ValueReadEventArgs(string key, object value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }

        public object Value { get; }
    }
}
