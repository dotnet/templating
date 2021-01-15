using System;

namespace Microsoft.TemplateEngine.Abstractions.Mount
{
    public interface IMountPoint : IDisposable
    {
        string AbsoluteUri { get; }

        IDirectory Root { get; }

        IEngineEnvironmentSettings EnvironmentSettings { get; }

        IFile FileInfo(string fullPath);

        IDirectory DirectoryInfo(string fullPath);

        IFileSystemInfo FileSystemInfo(string fullPath);
    }
}
