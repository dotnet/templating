// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Abstractions.PhysicalFileSystem;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Utils;

namespace Microsoft.TemplateEngine.Core.Util
{
    public class Orchestrator : IOrchestrator, IOrchestrator2
    {
        public void Run(string runSpecPath, ILogger logger, IPhysicalFileSystem fileSystem, string sourceDir, string targetDir)
        {
            IGlobalRunSpec spec;
            using (Stream stream = fileSystem.OpenRead(runSpecPath))
            {
                spec = RunSpecLoader(stream);
                EngineConfig config = new EngineConfig(logger, EngineConfig.DefaultWhitespaces, EngineConfig.DefaultLineEndings, spec.RootVariableCollection);
                IProcessor processor = Processor.Create(config, spec.Operations);
                stream.Position = 0;
                using (MemoryStream ms = new MemoryStream())
                {
                    processor.Run(stream, ms);
                    ms.Position = 0;
                    spec = RunSpecLoader(ms);
                }
            }

            RunInternal(logger, fileSystem, sourceDir, targetDir, spec);
        }

        public IReadOnlyList<IFileChange2> GetFileChanges(string runSpecPath, ILogger logger, IPhysicalFileSystem fileSystem, string sourceDir, string targetDir)
        {
            IGlobalRunSpec spec;
            using (Stream stream = fileSystem.OpenRead(runSpecPath))
            {
                spec = RunSpecLoader(stream);
                EngineConfig config = new EngineConfig(logger, EngineConfig.DefaultWhitespaces, EngineConfig.DefaultLineEndings, spec.RootVariableCollection);
                IProcessor processor = Processor.Create(config, spec.Operations);
                stream.Position = 0;
                using (MemoryStream ms = new MemoryStream())
                {
                    processor.Run(stream, ms);
                    ms.Position = 0;
                    spec = RunSpecLoader(ms);
                }
            }

            return GetFileChangesInternal(logger, fileSystem, sourceDir, targetDir, spec);
        }

        public void Run(IGlobalRunSpec spec, ILogger logger, IPhysicalFileSystem fileSystem, string sourceDir, string targetDir)
        {
            RunInternal(logger, fileSystem, sourceDir, targetDir, spec);
        }

        public IReadOnlyList<IFileChange2> GetFileChanges(IGlobalRunSpec spec, ILogger logger, IPhysicalFileSystem fileSystem, string sourceDir, string targetDir)
        {
            return GetFileChangesInternal(logger, fileSystem, sourceDir, targetDir, spec);
        }

        protected virtual IGlobalRunSpec RunSpecLoader(Stream runSpec)
        {
            return null;
        }

        protected virtual bool TryGetBufferSize(string sourceFile, out int bufferSize)
        {
            bufferSize = -1;
            return false;
        }

        protected virtual bool TryGetFlushThreshold(string sourceFile, out int threshold)
        {
            threshold = -1;
            return false;
        }

        private static List<KeyValuePair<IPathMatcher, IProcessor>> CreateFileGlobProcessors(ILogger logger, IGlobalRunSpec spec)
        {
            List<KeyValuePair<IPathMatcher, IProcessor>> processorList = new List<KeyValuePair<IPathMatcher, IProcessor>>();

            if (spec.Special != null)
            {
                foreach (KeyValuePair<IPathMatcher, IRunSpec> runSpec in spec.Special)
                {
                    IReadOnlyList<IOperationProvider> operations = runSpec.Value.GetOperations(spec.Operations);
                    EngineConfig config = new EngineConfig(logger, EngineConfig.DefaultWhitespaces, EngineConfig.DefaultLineEndings, spec.RootVariableCollection, runSpec.Value.VariableFormatString);
                    IProcessor processor = Processor.Create(config, operations);

                    processorList.Add(new KeyValuePair<IPathMatcher, IProcessor>(runSpec.Key, processor));
                }
            }

            return processorList;
        }

        private static string CreateTargetDir(IPhysicalFileSystem fileSystem, string sourceRel, string targetDir, IGlobalRunSpec spec)
        {
            if (!spec.TryGetTargetRelPath(sourceRel, out string targetRel))
            {
                targetRel = sourceRel;
            }

            string targetPath = Path.Combine(targetDir, targetRel);
            string fullTargetDir = Path.GetDirectoryName(targetPath);
            fileSystem.CreateDirectory(fullTargetDir);

            return targetPath;
        }

        private IReadOnlyList<IFileChange2> GetFileChangesInternal(ILogger logger, IPhysicalFileSystem fileSystem, string sourceDir, string targetDir, IGlobalRunSpec spec)
        {
            EngineConfig cfg = new EngineConfig(logger, EngineConfig.DefaultWhitespaces, EngineConfig.DefaultLineEndings, spec.RootVariableCollection);
            IProcessor fallback = Processor.Create(cfg, spec.Operations);

            List<IFileChange2> changes = new List<IFileChange2>();
            List<KeyValuePair<IPathMatcher, IProcessor>> fileGlobProcessors = CreateFileGlobProcessors(logger, spec);

            foreach (string file in fileSystem.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string sourceRel = fileSystem.PathRelativeTo(file, sourceDir);
                string fileName = Path.GetFileName(sourceRel);
                bool checkingDirWithPlaceholderFile = false;

                if (spec.IgnoreFileNames.Contains(fileName))
                {
                    // The placeholder file should never get copied / created / processed. It just causes the dir to get created if needed.
                    // The change checking / reporting is different, setting this variable tracks it.
                    checkingDirWithPlaceholderFile = true;
                }

                foreach (IPathMatcher include in spec.Include)
                {
                    if (include.IsMatch(sourceRel))
                    {
                        bool excluded = false;
                        foreach (IPathMatcher exclude in spec.Exclude)
                        {
                            if (exclude.IsMatch(sourceRel))
                            {
                                excluded = true;
                                break;
                            }
                        }

                        if (!excluded)
                        {
                            if (!spec.TryGetTargetRelPath(sourceRel, out string targetRel))
                            {
                                targetRel = sourceRel;
                            }

                            string targetPath = Path.Combine(targetDir, targetRel);

                            if (checkingDirWithPlaceholderFile)
                            {
                                targetPath = Path.GetDirectoryName(targetPath);
                                targetRel = Path.GetDirectoryName(targetRel);

                                if (fileSystem.DirectoryExists(targetPath))
                                {
                                    changes.Add(new FileChange(sourceRel, targetRel, ChangeKind.Overwrite));
                                }
                                else
                                {
                                    changes.Add(new FileChange(sourceRel, targetRel, ChangeKind.Create));
                                }
                            }
                            else if (fileSystem.FileExists(targetPath))
                            {
                                changes.Add(new FileChange(sourceRel, targetRel, ChangeKind.Overwrite));
                            }
                            else
                            {
                                changes.Add(new FileChange(sourceRel, targetRel, ChangeKind.Create));
                            }
                        }

                        break;
                    }
                }
            }

            return changes;
        }

        private void RunInternal(ILogger logger, IPhysicalFileSystem fileSystem, string sourceDir, string targetDir, IGlobalRunSpec spec)
        {
            EngineConfig cfg = new EngineConfig(logger, EngineConfig.DefaultWhitespaces, EngineConfig.DefaultLineEndings, spec.RootVariableCollection);
            IProcessor fallback = Processor.Create(cfg, spec.Operations);

            List<KeyValuePair<IPathMatcher, IProcessor>> fileGlobProcessors = CreateFileGlobProcessors(logger, spec);

            foreach (string file in fileSystem.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string sourceRel = fileSystem.PathRelativeTo(file, sourceDir);
                string fileName = Path.GetFileName(sourceRel);
                bool checkingDirWithPlaceholderFile = false;

                if (spec.IgnoreFileNames.Contains(fileName))
                {
                    // The placeholder file should never get copied / created / processed. It just causes the dir to get created if needed.
                    // The change checking / reporting is different, setting this variable tracks it.
                    checkingDirWithPlaceholderFile = true;
                }

                foreach (IPathMatcher include in spec.Include)
                {
                    if (include.IsMatch(sourceRel))
                    {
                        bool excluded = false;
                        foreach (IPathMatcher exclude in spec.Exclude)
                        {
                            if (exclude.IsMatch(sourceRel))
                            {
                                excluded = true;
                                break;
                            }
                        }

                        if (!excluded)
                        {
                            bool copy = false;
                            foreach (IPathMatcher copyOnly in spec.CopyOnly)
                            {
                                if (copyOnly.IsMatch(sourceRel))
                                {
                                    copy = true;
                                    break;
                                }
                            }

                            if (checkingDirWithPlaceholderFile)
                            {
                                CreateTargetDir(fileSystem, sourceRel, targetDir, spec);
                            }
                            else if (!copy)
                            {
                                ProcessFile(file, sourceRel, targetDir, fileSystem, spec, fallback, fileGlobProcessors);
                            }
                            else
                            {
                                string targetPath = CreateTargetDir(fileSystem, sourceRel, targetDir, spec);

                                using (Stream sourceStream = fileSystem.OpenRead(file))
                                using (Stream targetStream = fileSystem.CreateFile(targetPath))
                                {
                                    sourceStream.CopyTo(targetStream);
                                }
                            }
                        }

                        break;
                    }
                }
            }
        }

        private void ProcessFile(string sourceFile, string sourceRel, string targetDir, IPhysicalFileSystem fileSystem, IGlobalRunSpec spec, IProcessor fallback, IEnumerable<KeyValuePair<IPathMatcher, IProcessor>> fileGlobProcessors)
        {
            IProcessor runner = fileGlobProcessors.FirstOrDefault(x => x.Key.IsMatch(sourceRel)).Value ?? fallback;
            if (runner == null)
            {
                throw new InvalidOperationException("At least one of [runner] or [fallback] cannot be null");
            }

            if (!spec.TryGetTargetRelPath(sourceRel, out string targetRel))
            {
                targetRel = sourceRel;
            }

            string targetPath = Path.Combine(targetDir, targetRel);
            //TODO: Update context with the current file & such here

            bool customBufferSize = TryGetBufferSize(sourceFile, out int bufferSize);
            bool customFlushThreshold = TryGetFlushThreshold(sourceFile, out int flushThreshold);
            string fullTargetDir = Path.GetDirectoryName(targetPath);
            fileSystem.CreateDirectory(fullTargetDir);

            try
            {
                using (Stream source = fileSystem.OpenRead(sourceFile))
                using (Stream target = fileSystem.CreateFile(targetPath))
                {
                    if (!customBufferSize)
                    {
                        runner.Run(source, target);
                    }
                    else
                    {
                        if (!customFlushThreshold)
                        {
                            runner.Run(source, target, bufferSize);
                        }
                        else
                        {
                            runner.Run(source, target, bufferSize, flushThreshold);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //TODO: add FullPath to IPhysicalFileSystem and use here
                throw new ContentGenerationException($"Error while processing file {sourceFile}", ex);
            }
        }
    }
}
