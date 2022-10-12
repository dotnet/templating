// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;

namespace Microsoft.TemplateEngine.Edge.Mount.FileSystem
{
    public sealed class FileSystemMountPointFactory : IMountPointFactory
    {
        internal static readonly Guid s_factoryId = new Guid("8C19221B-DEA3-4250-86FE-2D4E189A11D2");

        Guid IIdentifiedComponent.Id => s_factoryId;

        bool IMountPointFactory.TryMount(IEngineEnvironmentSettings environmentSettings, IMountPoint? parent, string mountPointUri, out IMountPoint? mountPoint)
        {
            if (!Uri.TryCreate(mountPointUri, UriKind.Absolute, out var uri))
            {
                mountPoint = null;
                return false;
            }

            if (!uri.IsFile)
            {
                mountPoint = null;
                return false;
            }

            if (parent != null || !environmentSettings.Host.FileSystem.DirectoryExists(uri.LocalPath))
            {
                mountPoint = null;
                return false;
            }

            mountPoint = new FileSystemMountPoint(environmentSettings, mountPointUri, uri.LocalPath);
            return true;
        }
    }
}
