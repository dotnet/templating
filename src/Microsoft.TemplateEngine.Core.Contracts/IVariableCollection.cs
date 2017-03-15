﻿using System.Collections.Generic;

namespace Microsoft.TemplateEngine.Core.Contracts
{
    public interface IVariableCollection : IDictionary<string, object>
    {
        IVariableCollection Parent { get; set; }

        event KeysChangedEventHander KeysChanged;

        event ValueReadEventHander ValueRead;
    }
}
