using System;
using System.IO;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;

namespace Microsoft.TemplateEngine.Edge.Mount.FileSystem
{
    public class FileSystemMountPoint : IMountPoint
    {
        private Paths _paths;
        public string Place { get; }

        public FileSystemMountPoint(IEngineEnvironmentSettings environmentSettings, IMountPoint parent, string place)
        {
            EnvironmentSettings = environmentSettings;
            _paths = new Paths(environmentSettings);
            Place = place;
            Root = new FileSystemDirectory(this, "/", "", place);
        }

        public IDirectory Root { get; }

        public IEngineEnvironmentSettings EnvironmentSettings { get; }

        public IFile FileInfo(string fullPath)
        {
            string realPath = Path.Combine(Place, fullPath.TrimStart('/'));

            if (!fullPath.StartsWith("/"))
            {
                fullPath = "/" + fullPath;
            }

            return new FileSystemFile(this, fullPath, _paths.Name(realPath), realPath);
        }

        public IDirectory DirectoryInfo(string fullPath)
        {
            string realPath = Path.Combine(Place, fullPath.TrimStart('/'));
            return new FileSystemDirectory(this, fullPath, _paths.Name(realPath), realPath);
        }

        public IFileSystemInfo FileSystemInfo(string fullPath)
        {
            string realPath = Path.Combine(Place, fullPath.TrimStart('/'));

            if (EnvironmentSettings.Host.FileSystem.DirectoryExists(realPath))
            {
                return new FileSystemDirectory(this, fullPath, _paths.Name(realPath), realPath);
            }

            return new FileSystemFile(this, fullPath, _paths.Name(realPath), realPath);
        }

        public void Dispose()
        {

        }

        public IMountPoint Parent { get; }

        public Guid MountPointFactoryId => FileSystemMountPointFactory.FactoryId;

        public string AbsoluteUri => Place;
    }
}
