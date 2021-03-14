// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.TemplateEngine.TemplateLocalizer
{
    internal sealed class TemplateJsonProvider
    {
        /// <summary>
        /// Creates an instance of <see cref="TemplateJsonProvider"/>.
        /// </summary>
        /// <param name="path">A path to a template.jsonfile or a path to a directory that contains a template.json file.</param>
        public TemplateJsonProvider(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Gets the path used by this provider to search for template.json files.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Using the <see cref="Path"/>, finds and returns all the template.json files.
        /// </summary>
        /// <param name="searchSubfolders">Indicates weather the subdirectories should be searched
        /// in the case that <see cref="Path"/> points to a directory. This parameter has no effect
        /// if <see cref="Path"/> points to a file.</param>
        /// <returns></returns>
        public IEnumerable<FileInfo> GetTemplateJsonFiles(bool searchSubfolders)
        {
            if (File.Exists(Path))
            {
                yield return new FileInfo(Path);
                yield break;
            }

            if (!Directory.Exists(Path))
            {
                // This path neither points to a file nor to a directory.
                yield break;
            }

            if (!searchSubfolders)
            {
                string filePath = System.IO.Path.Combine(Path, "template.json");
                if (File.Exists(filePath))
                {
                    yield return new FileInfo(filePath);
                }

                yield break;
            }

            // Search directory and all subfolders
            foreach (string filePath in Directory.EnumerateFiles(Path, "*.json", SearchOption.AllDirectories))
            {
                yield return new FileInfo(filePath);
            }
        }
    }
}
