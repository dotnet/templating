// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;

namespace Microsoft.TemplateEngine.Edge.Mount.FileSystem
{
    internal class FileSystemMountPointFactory : IMountPointFactory
    {
        internal static readonly Guid FactoryId = new Guid("8C19221B-DEA3-4250-86FE-2D4E189A11D2");

        public Guid Id => FactoryId;

        public bool CanMount(IEngineEnvironmentSettings environmentSettings, IMountPoint? parent, string mountPointUri)
        {
            return InnerCanMount(environmentSettings, parent, mountPointUri, out _);
        }

        public bool TryMount(IEngineEnvironmentSettings environmentSettings, IMountPoint? parent, string mountPointUri, out IMountPoint? mountPoint)
        {
            if (InnerCanMount(environmentSettings, parent, mountPointUri, out var path))
            {
                mountPoint = new FileSystemMountPoint(environmentSettings, parent, mountPointUri, path!);
                return true;
            }
            else
            {
                mountPoint = null;
                return false;
            }
        }

        private static bool InnerCanMount(IEngineEnvironmentSettings environmentSettings, IMountPoint? parent, string mountPointUri, out string? path)
        {
            path = null;
            if (!Uri.TryCreate(mountPointUri, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (!uri.IsFile)
            {
                return false;
            }

            if (parent != null || !environmentSettings.Host.FileSystem.DirectoryExists(uri.LocalPath))
            {
                return false;
            }

            path = uri.LocalPath;
            return true;
        }
    }
}
