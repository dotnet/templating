// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    internal class RunnableProjectOrchestrator : IOrchestrator, IOrchestrator2
    {
        private readonly IOrchestrator2 _basicOrchestrator;

        public RunnableProjectOrchestrator(IOrchestrator2 basicOrchestrator)
        {
            _basicOrchestrator = basicOrchestrator;
        }

        public IReadOnlyList<IFileChange2> GetFileChanges(string runSpecPath, ILogger logger, IPhysicalFileSystem fileSystem, string sourceDir, string targetDir)
        {
            return _basicOrchestrator.GetFileChanges(runSpecPath, logger, fileSystem, sourceDir, targetDir);
        }

        public IReadOnlyList<IFileChange2> GetFileChanges(IGlobalRunSpec spec, ILogger logger, IPhysicalFileSystem fileSystem, string sourceDir, string targetDir)
        {
            return _basicOrchestrator.GetFileChanges(spec, logger, fileSystem, sourceDir, targetDir);
        }

        public void Run(string runSpecPath, ILogger logger, IPhysicalFileSystem fileSystem, string sourceDir, string targetDir)
        {
            _basicOrchestrator.Run(runSpecPath, logger, fileSystem, sourceDir, targetDir);
        }

        public void Run(IGlobalRunSpec runSpec, ILogger logger, IPhysicalFileSystem fileSystem, string directoryInfo, string target)
        {
            _basicOrchestrator.Run(runSpec, logger, fileSystem, directoryInfo, target);
        }
    }
}
