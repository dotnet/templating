// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;

namespace Microsoft.TemplateEngine.Core.Contracts
{
    public interface IOrchestrator
    {
        void Run(string runSpecPath, ILogger logger, IPhysicalFileSystem fileSystem, string sourceDir, string targetDir);

        void Run(IGlobalRunSpec spec, ILogger logger, IPhysicalFileSystem fileSystem, string sourceDir, string targetDir);
    }

    public interface IOrchestrator2
    {
        void Run(string runSpecPath, ILogger logger, IPhysicalFileSystem fileSystem, string sourceDir, string targetDir);

        void Run(IGlobalRunSpec spec, ILogger logger, IPhysicalFileSystem fileSystem, string sourceDir, string targetDir);

        IReadOnlyList<IFileChange2> GetFileChanges(string runSpecPath, ILogger logger, IPhysicalFileSystem fileSystem, string sourceDir, string targetDir);

        IReadOnlyList<IFileChange2> GetFileChanges(IGlobalRunSpec spec, ILogger logger, IPhysicalFileSystem fileSystem, string sourceDir, string targetDir);
    }
}
