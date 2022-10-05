// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.TemplateEngine.Authoring.TemplateVerifier
{
    internal interface IFileSystem
    {
        public bool DirectoryExists(string path);

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);

        Task<string> ReadAllTextAsync(string path);

        Task WriteAllTextAsync(string path, string? contents);

        void CreateDirectory(string path);

        void DeleteDirectory(string path, bool recursive);

    }
}
