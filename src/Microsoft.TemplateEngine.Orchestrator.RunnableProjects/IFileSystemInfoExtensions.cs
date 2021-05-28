// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.IO;
using Microsoft.TemplateEngine.Abstractions.Mount;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects
{
    internal static class IFileSystemInfoExtensions
    {
        /// <summary>
        /// Returns full path to <paramref name="fileSystemInfo"/> including mount point URI.
        /// </summary>
        /// <param name="fileSystemInfo">the file system info to get full path for.</param>
        /// <returns>
        /// Full path to <paramref name="fileSystemInfo"/> including mount point URI.
        /// If mount point is not a directory, the path is returned as 'mount point URI(path inside mount point)'.</returns>
        internal static string GetFullPath (this IFileSystemInfo fileSystemInfo)
        {
            if (fileSystemInfo.MountPoint.EnvironmentSettings.Host.FileSystem.DirectoryExists(fileSystemInfo.MountPoint.MountPointUri))
            {
                //mount point is a directory, combine paths
                return Path.Combine(fileSystemInfo.MountPoint.MountPointUri, fileSystemInfo.FullPath.Trim('/', '\\'));
            }

            //assuming file or anything else
            return $"{fileSystemInfo.MountPoint.MountPointUri}({fileSystemInfo.FullPath})";
        }
    }
}
