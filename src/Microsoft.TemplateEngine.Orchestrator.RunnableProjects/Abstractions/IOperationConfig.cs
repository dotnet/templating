﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Abstractions
{
    public interface IOperationConfig : IIdentifiedComponent
    {
        string Key { get; }

        IEnumerable<IOperationProvider> ConfigureFromJson(string rawConfiguration, IDirectory templateRoot);
    }
}
