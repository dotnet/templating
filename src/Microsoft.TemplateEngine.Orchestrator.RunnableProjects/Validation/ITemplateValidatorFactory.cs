// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Validation
{
    [Flags]
    internal enum ValidationScope
    {
        None = 0,
        Scanning = 1,
        Instantiation = 2,
    }

    internal interface ITemplateValidatorFactory : IIdentifiedComponent
    {
        ValidationScope Scope { get; }

        Task<ITemplateValidator> CreateValidatorAsync(IEngineEnvironmentSettings engineEnvironmentSettings, CancellationToken cancellationToken);
    }
}
