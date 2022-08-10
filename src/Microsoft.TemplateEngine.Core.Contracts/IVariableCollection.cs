﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Core.Contracts
{
    public interface IVariableCollection : IDictionary<string, object>
    {
        event KeysChangedEventHander KeysChanged;

        event ValueReadEventHander ValueRead;

        IVariableCollection? Parent { get; set; }
    }
}
