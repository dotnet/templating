// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;

namespace Microsoft.TemplateEngine.Core.Contracts
{
    public interface IOrchestrator
    {
        void Run(string runSpecPath, string sourceDir, string targetDir);

        void Run(IGlobalRunSpec spec, string sourceDir, string targetDir);
    }

    public interface IOrchestrator2
    {
        void Run(string runSpecPath, string sourceDir, string targetDir);

        void Run(IGlobalRunSpec spec, string sourceDir, string targetDir);

        IReadOnlyList<IFileChange2> GetFileChanges(string runSpecPath, string sourceDir, string targetDir);

        IReadOnlyList<IFileChange2> GetFileChanges(IGlobalRunSpec spec, string sourceDir, string targetDir);
    }
}
