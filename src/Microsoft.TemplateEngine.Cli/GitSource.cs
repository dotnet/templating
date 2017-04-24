using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.TemplateEngine.Cli
{
    internal class GitSource
    {
        public string GitUrl { get; set; }
        public string SubFolder { get; set; }
        public string RepositoryName { get; set; }

        public GitSource(string gitUrl, string subFolder, string repoName)
        {
            GitUrl = gitUrl;
            SubFolder = subFolder;
            RepositoryName = repoName;
        }

        public static bool TryParseGitSource(string spec, out GitSource package)
        {
            package = null;
            int gitIndex = -1;

            if (string.IsNullOrEmpty(spec) || (gitIndex = spec.IndexOf(".git", StringComparison.OrdinalIgnoreCase)) < 0)
            {
                return false;
            }
            else
            {
                var index = gitIndex + 4;
                var indexOfLastSlashBeforeGit = -1;
                var indexOfSlash = -1;
                while ((indexOfSlash = spec.IndexOf('/', indexOfLastSlashBeforeGit + 1)) < index && indexOfSlash != -1)
                {
                    indexOfLastSlashBeforeGit = indexOfSlash;
                }

                var gitUrl = spec.Substring(0, index);
                var subFolder = spec.Substring(index);
                subFolder = subFolder.Trim('/');
                var repoName = gitUrl.Substring(indexOfLastSlashBeforeGit + 1).Replace(".git", string.Empty);
                package = new GitSource(gitUrl, subFolder, repoName);
                return true;
            }
        }
    }
}
