// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Edge.Settings;

namespace Microsoft.TemplateEngine.Edge.Mount.FileSystem
{
    /// <summary>
    /// Mount point implementation for file system directory.
    /// </summary>
    internal class FileSystemMountPoint : IMountPoint
    {
        private SettingsFilePaths _paths;

        internal FileSystemMountPoint(IEngineEnvironmentSettings environmentSettings, IMountPoint parent, string mountPointUri, string mountPointRootPath)
        {
            MountPointUri = mountPointUri;
            MountPointRootPath = mountPointRootPath;
            EnvironmentSettings = environmentSettings;
            _paths = new SettingsFilePaths(environmentSettings);
            Root = new FileSystemDirectory(this, "/", "", MountPointRootPath);
        }

        public IDirectory Root { get; }

        public IEngineEnvironmentSettings EnvironmentSettings { get; }

        public IMountPoint Parent { get; }

        public Guid MountPointFactoryId => FileSystemMountPointFactory.FactoryId;

        public string MountPointUri { get; }

        /// <summary>
        /// Returns full path of the mounted directory.
        /// </summary>
        internal string MountPointRootPath { get; }

        public IFile FileInfo(string path)
        {
            string fullPath = Path.Combine(MountPointRootPath, path.TrimStart('/'));

            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }

            return new FileSystemFile(this, path, _paths.Name(fullPath), fullPath);
        }

        public IDirectory DirectoryInfo(string path)
        {
            string fullPath = Path.Combine(MountPointRootPath, path.TrimStart('/'));
            return new FileSystemDirectory(this, path, _paths.Name(fullPath), fullPath);
        }

        public IFileSystemInfo FileSystemInfo(string path)
        {
            string fullPath = Path.Combine(MountPointRootPath, path.TrimStart('/'));

            if (EnvironmentSettings.Host.FileSystem.DirectoryExists(fullPath))
            {
                return new FileSystemDirectory(this, path, _paths.Name(fullPath), fullPath);
            }

            return new FileSystemFile(this, path, _paths.Name(fullPath), fullPath);
        }

        public void Dispose()
        {
        }
    }
}
