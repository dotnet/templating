﻿using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    public class RunnableProjectOrchestrator : IOrchestrator
    {
        private readonly IOrchestrator _basicOrchestrator;

        public RunnableProjectOrchestrator(IOrchestrator basicOrchestrator)
        {
            _basicOrchestrator = basicOrchestrator;
        }

        public IReadOnlyList<IFileChange> GetFileChanges(string runSpecPath, IDirectory sourceDir, string targetDir)
        {
            return _basicOrchestrator.GetFileChanges(runSpecPath, sourceDir, targetDir);
        }

        public IReadOnlyList<IFileChange> GetFileChanges(IGlobalRunSpec spec, IDirectory sourceDir, string targetDir)
        {
            return _basicOrchestrator.GetFileChanges(spec, sourceDir, targetDir);
        }

        public void Run(string runSpecPath, IDirectory sourceDir, string targetDir)
        {
            _basicOrchestrator.Run(runSpecPath, sourceDir, targetDir);
        }

        public void Run(IGlobalRunSpec runSpec, IDirectory directoryInfo, string target)
        {
            _basicOrchestrator.Run(runSpec, directoryInfo, target);
        }
    }
}
