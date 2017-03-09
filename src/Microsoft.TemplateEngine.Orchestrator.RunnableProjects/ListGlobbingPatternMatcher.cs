using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Core.Contracts;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    public class ListGlobbingPatternMatcher : IPathMatcher
    {
        public ListGlobbingPatternMatcher(IList<string> patternList)
        {
            List<IPathMatcher> pathMatchers = new List<IPathMatcher>();

            foreach (string pattern in patternList)
            {
                pathMatchers.Add(new GlobbingPatternMatcher(pattern));
            }

            _pathMatchers = pathMatchers;
        }

        private readonly IReadOnlyList<IPathMatcher> _pathMatchers;

        public string Pattern
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsMatch(string path)
        {
            foreach (IPathMatcher matcher in _pathMatchers)
            {
                if (matcher.IsMatch(path))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
