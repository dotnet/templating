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
                int index = gitIndex + 4;
                int indexOfLastSlashBeforeGit = -1;
                int indexOfSlash = -1;
                while ((indexOfSlash = spec.IndexOf('/', indexOfLastSlashBeforeGit + 1)) < index && indexOfSlash != -1)
                {
                    indexOfLastSlashBeforeGit = indexOfSlash;
                }

                string gitUrl = spec.Substring(0, index);
                string subFolder = spec.Substring(index);
                subFolder = subFolder.Trim('/');
                string repoName = gitUrl.Substring(indexOfLastSlashBeforeGit + 1).Replace(".git", string.Empty);
                package = new GitSource(gitUrl, subFolder, repoName);
                return true;
            }
        }
    }
}
