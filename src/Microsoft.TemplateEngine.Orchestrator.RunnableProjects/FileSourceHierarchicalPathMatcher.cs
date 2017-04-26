﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    [Flags]
    public enum FileDispositionStates
    {
        None = 0,
        Include = 0x01,
        CopyOnly = 0x02,
        Exclude = 0x04
    }

    public static class EnumExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has(this FileDispositionStates value, FileDispositionStates check)
        {
            return (value & check) == check;
        }
    }

    public class FileSourceHierarchicalPathMatcher
    {
        public FileSourceHierarchicalPathMatcher(FileSourceMatchInfo matchInfo)
        {
            List<FileSourceEvaluablePathMatcher> evaluators = new List<FileSourceEvaluablePathMatcher>();

            for (int i = matchInfo.Modifiers.Count - 1; i >= 0; i--)
            {
                evaluators.Add(new FileSourceEvaluablePathMatcher(matchInfo.Modifiers[i]));
            }

            evaluators.Add(new FileSourceEvaluablePathMatcher(matchInfo.TopLevelEvaluable));
            _evaluators = evaluators;

            _cachedEvaluatedPath = string.Empty;
            _cachedStatesForFile = FileDispositionStates.None;
        }

        private IReadOnlyList<FileSourceEvaluablePathMatcher> _evaluators;

        // Caching the most recent result.
        private string _cachedEvaluatedPath;
        private FileDispositionStates _cachedStatesForFile;

        public FileDispositionStates Evaluate(string path)
        {
            if (!string.Equals(path, _cachedEvaluatedPath, StringComparison.Ordinal))
            {
                _cachedStatesForFile = EvaluatePath(path);
                _cachedEvaluatedPath = path;
            }

            return _cachedStatesForFile;
        }

        private FileDispositionStates EvaluatePath(string path)
        {
            FileDispositionStates continuationReason = FileDispositionStates.None;

            foreach (FileSourceEvaluablePathMatcher evaluator in _evaluators)
            {
                FileDispositionStates state = evaluator.Evaluate(path);

                if (state.Has(FileDispositionStates.Exclude))
                {
                    return  FileDispositionStates.Exclude;
                }
                else if (state.Has(FileDispositionStates.CopyOnly))
                {
                    if (state.Has(FileDispositionStates.Include))
                    {
                        return FileDispositionStates.CopyOnly;
                    }

                    continuationReason = FileDispositionStates.CopyOnly;
                }
                else if (state.Has(FileDispositionStates.Include))
                {
                    if (continuationReason == FileDispositionStates.CopyOnly)
                    {
                        return FileDispositionStates.CopyOnly;
                    }

                    return FileDispositionStates.Include;
                }
            }

            return FileDispositionStates.None;
        }

        private class FileSourceEvaluablePathMatcher
        {
            public FileSourceEvaluablePathMatcher(FileSourceEvaluable evaluable)
            {
                _include = SetupMatcherList(evaluable.Include);
                _exclude = SetupMatcherList(evaluable.Exclude);
                _copyOnly = SetupMatcherList(evaluable.CopyOnly);
            }

            private IReadOnlyList<IPathMatcher> _include;

            private IReadOnlyList<IPathMatcher> _exclude;

            private IReadOnlyList<IPathMatcher> _copyOnly;

            public FileDispositionStates Evaluate(string path)
            {
                if (_exclude.Any(x => x.IsMatch(path)))
                {
                    return FileDispositionStates.Exclude;
                }

                bool include = _include.Any(x => x.IsMatch(path));

                if (_copyOnly.Any(x => x.IsMatch(path)))
                {
                    if (include)
                    {
                        return FileDispositionStates.CopyOnly | FileDispositionStates.Include;
                    }
                    else
                    {
                        return FileDispositionStates.CopyOnly;
                    }
                }

                if (include)
                {
                    return FileDispositionStates.Include;
                }

                return FileDispositionStates.None;
            }

            private static IReadOnlyList<IPathMatcher> SetupMatcherList(IReadOnlyList<string> fileSources)
            {
                int expect = fileSources?.Count ?? 0;
                List<IPathMatcher> paths = new List<IPathMatcher>(expect);
                if (fileSources != null && expect > 0)
                {
                    foreach (string source in fileSources)
                    {
                        paths.Add(new GlobbingPatternMatcher(source, false));
                    }
                }

                return paths;
            }
        }
    }
}
